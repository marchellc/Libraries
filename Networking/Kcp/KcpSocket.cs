using System;
using System.Collections.Generic;

namespace Networking.Kcp
{
    public class KcpSocket
    {
        public const int RTO_NDL = 30;           
        public const int RTO_MIN = 100;     
        public const int RTO_DEF = 200;          
        public const int RTO_MAX = 60000;     
        
        public const int CMD_PUSH = 81;       
        public const int CMD_ACK  = 82;         
        public const int CMD_WASK = 83;   
        public const int CMD_WINS = 84;     
        
        public const int ASK_SEND = 1;        
        public const int ASK_TELL = 2;   

        public const int WND_SND = 32;           
        public const int WND_RCV = 128;   
        
        public const int MTU_DEF = 1200;          
        public const int ACK_FAST = 3;
        public const int INTERVAL = 100;
        public const int OVERHEAD = 24;
        public const int FRG_MAX = byte.MaxValue;  
        public const int DEADLINK = 20;      
        
        public const int THRESH_INIT = 2;
        public const int THRESH_MIN = 2;
        public const int PROBE_INIT = 7000;        
        public const int PROBE_LIMIT = 120000; 
        public const int FASTACK_LIMIT = 5;        

        internal int state;
        readonly uint conv;        
        internal uint mtu;
        internal uint mss;         
        internal uint snd_una;     
        internal uint snd_nxt;      
        internal uint rcv_nxt;      
        internal uint ssthresh;   
        internal int rx_rttval;    
        internal int rx_srtt;       
        internal int rx_rto;
        internal int rx_minrto;
        internal uint snd_wnd;     
        internal uint rcv_wnd;       
        internal uint rmt_wnd;    
        internal uint cwnd;         
        internal uint probe;
        internal uint interval;
        internal uint ts_flush;   
        internal uint xmit;
        internal uint nodelay;    
        internal bool updated;
        internal uint ts_probe;   
        internal uint probe_wait;
        internal uint dead_link;    
        internal uint incr;
        internal uint current;  

        internal int fastresend;
        internal int fastlimit;

        internal bool nocwnd;    
        
        internal readonly Queue<KcpSegment> snd_queue = new Queue<KcpSegment>(16);
        internal readonly Queue<KcpSegment> rcv_queue = new Queue<KcpSegment>(16); 

        internal readonly List<KcpSegment> snd_buf = new List<KcpSegment>(16);
        internal readonly List<KcpSegment> rcv_buf = new List<KcpSegment>(16);
        internal readonly List<KcpAck> acklist = new List<KcpAck>(16);

        internal byte[] buffer;

        readonly Action<byte[], int> output;

        readonly KcpPool<KcpSegment> KcpSegmentPool = new KcpPool<KcpSegment>(
            () => new KcpSegment(),
            (KcpSegment) => KcpSegment.Reset(),
            32
        );

        public KcpSocket(uint conv, Action<byte[], int> output)
        {
            this.conv = conv;
            this.output = output;

            snd_wnd = WND_SND;
            rcv_wnd = WND_RCV;
            rmt_wnd = WND_RCV;
            mtu = MTU_DEF;
            mss = mtu - OVERHEAD;
            rx_rto = RTO_DEF;
            rx_minrto = RTO_MIN;
            interval = INTERVAL;
            ts_flush = INTERVAL;
            ssthresh = THRESH_INIT;
            fastlimit = FASTACK_LIMIT;
            dead_link = DEADLINK;

            buffer = new byte[(mtu + OVERHEAD) * 3];
        }

        KcpSegment KcpSegmentNew() 
            => KcpSegmentPool.Take();

        void KcpSegmentDelete(KcpSegment seg) 
            => KcpSegmentPool.Return(seg);

        public int WaitSnd 
            => snd_buf.Count + snd_queue.Count;

        internal uint WndUnused()
        {
            if (rcv_queue.Count < rcv_wnd)
                return rcv_wnd - (uint)rcv_queue.Count;
            return 0;
        }

        public int Receive(byte[] buffer, int len)
        {
            if (len < 0)
                throw new NotSupportedException("Receive ispeek for negative len is not supported!");

            if (rcv_queue.Count == 0)
                return -1;

            if (len < 0) len = -len;

            int peeksize = PeekSize();

            if (peeksize < 0)
                return -2;

            if (peeksize > len)
                return -3;

            bool recover = rcv_queue.Count >= rcv_wnd;

            int offset = 0;
            len = 0;

            while (rcv_queue.Count > 0)
            {
                KcpSegment seg = rcv_queue.Dequeue();
                Buffer.BlockCopy(seg.data.GetBuffer(), 0, buffer, offset, (int)seg.data.Position);
                offset += (int)seg.data.Position;

                len += (int)seg.data.Position;
                uint fragment = seg.frg;
                KcpSegmentDelete(seg);

                if (fragment == 0)
                    break;
            }

            int removed = 0;

            foreach (KcpSegment seg in rcv_buf)
            {
                if (seg.sn == rcv_nxt && rcv_queue.Count < rcv_wnd)
                {
                    ++removed;

                    rcv_queue.Enqueue(seg);
                    rcv_nxt++;
                }
                else
                {
                    break;
                }
            }

            rcv_buf.RemoveRange(0, removed);

            if (rcv_queue.Count < rcv_wnd && recover)
                probe |= ASK_TELL;

            return len;
        }

        public int PeekSize()
        {
            int length = 0;

            if (rcv_queue.Count == 0) 
                return -1;

            KcpSegment seq = rcv_queue.Peek();

            if (seq.frg == 0) 
                return (int)seq.data.Position;

            if (rcv_queue.Count < seq.frg + 1) 
                return -1;

            foreach (KcpSegment seg in rcv_queue)
            {
                length += (int)seg.data.Position;

                if (seg.frg == 0) 
                    break;
            }

            return length;
        }

        public int Send(byte[] buffer, int offset, int len)
        {
            int count;

            if (len < 0) 
                return -1;

            if (len <= mss) 
                count = 1;
            else 
                count = (int)((len + mss - 1) / mss);

            if (count > FRG_MAX)
                throw new Exception($"Send len={len} requires {count} fragments, but kcp can only handle up to {FRG_MAX} fragments.");

            if (count >= rcv_wnd) 
                return -2;

            if (count == 0) 
                count = 1;

            for (int i = 0; i < count; i++)
            {
                int size = len > (int)mss ? (int)mss : len;
                KcpSegment seg = KcpSegmentNew();

                if (len > 0)
                    seg.data.Write(buffer, offset, size);

                seg.frg = (uint)(count - i - 1);
                snd_queue.Enqueue(seg);
                offset += size;
                len -= size;
            }

            return 0;
        }

        void UpdateAck(int rtt) 
        {
            if (rx_srtt == 0)
            {
                rx_srtt = rtt;
                rx_rttval = rtt / 2;
            }
            else
            {
                int delta = rtt - rx_srtt;

                if (delta < 0) 
                    delta = -delta;

                rx_rttval = (3 * rx_rttval + delta) / 4;
                rx_srtt = (7 * rx_srtt + rtt) / 8;

                if (rx_srtt < 1) 
                    rx_srtt = 1;
            }

            int rto = rx_srtt + Math.Max((int)interval, 4 * rx_rttval);
            rx_rto = KcpUtils.Clamp(rto, rx_minrto, RTO_MAX);
        }

        internal void ShrinkBuf()
        {
            if (snd_buf.Count > 0)
            {
                KcpSegment seg = snd_buf[0];
                snd_una = seg.sn;
            }
            else
            {
                snd_una = snd_nxt;
            }
        }

        internal void ParseAck(uint sn)
        {
            if (KcpUtils.TimeDiff(sn, snd_una) < 0 || KcpUtils.TimeDiff(sn, snd_nxt) >= 0)
                return;

            for (int i = 0; i < snd_buf.Count; ++i)
            {
                KcpSegment seg = snd_buf[i];
                if (sn == seg.sn)
                {
                    snd_buf.RemoveAt(i);
                    KcpSegmentDelete(seg);
                    break;
                }

                if (KcpUtils.TimeDiff(sn, seg.sn) < 0)
                {
                    break;
                }
            }
        }

        internal void ParseUna(uint una)
        {
            int removed = 0;

            foreach (KcpSegment seg in snd_buf)
            {
                if (seg.sn < una)
                {
                    ++removed;
                    KcpSegmentDelete(seg);
                }
                else
                {
                    break;
                }
            }

            snd_buf.RemoveRange(0, removed);
        }

        internal void ParseFastack(uint sn, uint ts)
        {
            if (sn < snd_una)
                return;

            if (sn >= snd_nxt)
                return;

            foreach (KcpSegment seg in snd_buf)
            {
                if (sn < seg.sn)
                {
                    break;
                }
                else if (sn != seg.sn)
                {
#if !FASTACK_CONSERVE
                    seg.fastack++;
#else
                    if (KcpUtils.TimeDiff(ts, seg.ts) >= 0)
                        seg.fastack++;
#endif
                }
            }
        }

        void AckPush(uint sn, uint ts)
        {
            acklist.Add(new KcpAck { serialNumber = sn, timestamp = ts });
        }

        void ParseData(KcpSegment newseg)
        {
            uint sn = newseg.sn;

            if (KcpUtils.TimeDiff(sn, rcv_nxt + rcv_wnd) >= 0 ||
                KcpUtils.TimeDiff(sn, rcv_nxt) < 0)
            {
                KcpSegmentDelete(newseg);
                return;
            }

            InsertKcpSegmentInReceiveBuffer(newseg);
            MoveReceiveBufferReadyKcpSegmentsToQueue();
        }

        internal void InsertKcpSegmentInReceiveBuffer(KcpSegment newseg)
        {
            bool repeat = false; // 'duplicate'

            int i;
            for (i = rcv_buf.Count - 1; i >= 0; i--)
            {
                KcpSegment seg = rcv_buf[i];

                if (seg.sn == newseg.sn)
                {
                    repeat = true;
                    break;
                }

                if (KcpUtils.TimeDiff(newseg.sn, seg.sn) > 0)
                {
                    break;
                }
            }

            if (!repeat)
            {
                rcv_buf.Insert(i + 1, newseg);
            }
            else
            {
                KcpSegmentDelete(newseg);
            }
        }

        void MoveReceiveBufferReadyKcpSegmentsToQueue()
        {
            int removed = 0;

            foreach (KcpSegment seg in rcv_buf)
            {
                if (seg.sn == rcv_nxt && rcv_queue.Count < rcv_wnd)
                {
                    ++removed;
                    rcv_queue.Enqueue(seg);
                    rcv_nxt++;
                }
                else
                {
                    break;
                }
            }

            rcv_buf.RemoveRange(0, removed);
        }

        public int Input(byte[] data, int offset, int size)
        {
            uint prev_una = snd_una;
            uint maxack = 0;
            uint latest_ts = 0;
            int flag = 0;

            if (data == null || size < OVERHEAD) return -1;

            while (true)
            {
                if (size < OVERHEAD) 
                    break;

                offset += KcpUtils.Decode32U(data, offset, out uint conv_);

                if (conv_ != conv) 
                    return -1;

                offset += KcpUtils.Decode8u(data, offset, out byte cmd);
                offset += KcpUtils.Decode8u(data, offset, out byte frg);
                offset += KcpUtils.Decode16U(data, offset, out ushort wnd);
                offset += KcpUtils.Decode32U(data, offset, out uint ts);
                offset += KcpUtils.Decode32U(data, offset, out uint sn);
                offset += KcpUtils.Decode32U(data, offset, out uint una);
                offset += KcpUtils.Decode32U(data, offset, out uint len);

                size -= OVERHEAD;

                if (size < len || (int)len < 0) 
                    return -2;

                if (cmd != CMD_PUSH && cmd != CMD_ACK &&
                    cmd != CMD_WASK && cmd != CMD_WINS)
                    return -3;

                rmt_wnd = wnd;
                ParseUna(una);
                ShrinkBuf();

                if (cmd == CMD_ACK)
                {
                    if (KcpUtils.TimeDiff(current, ts) >= 0)
                    {
                        UpdateAck(KcpUtils.TimeDiff(current, ts));
                    }
                    ParseAck(sn);
                    ShrinkBuf();
                    if (flag == 0)
                    {
                        flag = 1;
                        maxack = sn;
                        latest_ts = ts;
                    }
                    else
                    {
                        if (KcpUtils.TimeDiff(sn, maxack) > 0)
                        {
#if !FASTACK_CONSERVE
                            maxack = sn;
                            latest_ts = ts;
#else
                            if (KcpUtils.TimeDiff(ts, latest_ts) > 0)
                            {
                                maxack = sn;
                                latest_ts = ts;
                            }
#endif
                        }
                    }
                }
                else if (cmd == CMD_PUSH)
                {
                    if (KcpUtils.TimeDiff(sn, rcv_nxt + rcv_wnd) < 0)
                    {
                        AckPush(sn, ts);
                        if (KcpUtils.TimeDiff(sn, rcv_nxt) >= 0)
                        {
                            KcpSegment seg = KcpSegmentNew();
                            seg.conv = conv_;
                            seg.cmd = cmd;
                            seg.frg = frg;
                            seg.wnd = wnd;
                            seg.ts = ts;
                            seg.sn = sn;
                            seg.una = una;
                            if (len > 0)
                            {
                                seg.data.Write(data, offset, (int)len);
                            }
                            ParseData(seg);
                        }
                    }
                }
                else if (cmd == CMD_WASK)
                {
                    probe |= ASK_TELL;
                }
                else if (cmd == CMD_WINS)
                {

                }
                else
                {
                    return -3;
                }

                offset += (int)len;
                size -= (int)len;
            }

            if (flag != 0)
            {
                ParseFastack(maxack, latest_ts);
            }

            if (KcpUtils.TimeDiff(snd_una, prev_una) > 0)
            {
                if (cwnd < rmt_wnd)
                {
                    if (cwnd < ssthresh)
                    {
                        cwnd++;
                        incr += mss;
                    }
                    else
                    {
                        if (incr < mss) incr = mss;
                        incr += (mss * mss) / incr + (mss / 16);
                        if ((cwnd + 1) * mss <= incr)
                        {
                            cwnd = (incr + mss - 1) / ((mss > 0) ? mss : 1);
                        }
                    }
                    if (cwnd > rmt_wnd)
                    {
                        cwnd = rmt_wnd;
                        incr = rmt_wnd * mss;
                    }
                }
            }

            return 0;
        }

        void MakeSpace(ref int size, int space)
        {
            if (size + space > mtu)
            {
                output(buffer, size);
                size = 0;
            }
        }

        void FlushBuffer(int size)
        {
            if (size > 0)
            {
                output(buffer, size);
            }
        }

        public void Flush()
        {
            int size  = 0;    
            bool lost = false; 

            if (!updated) 
                return;

            KcpSegment seg = KcpSegmentNew();

            seg.conv = conv;
            seg.cmd = CMD_ACK;
            seg.wnd = WndUnused();
            seg.una = rcv_nxt;

            foreach (KcpAck ack in acklist)
            {
                MakeSpace(ref size, OVERHEAD);
                seg.sn = ack.serialNumber;
                seg.ts = ack.timestamp;
                size += seg.Encode(buffer, size);
            }

            acklist.Clear();

            if (rmt_wnd == 0)
            {
                if (probe_wait == 0)
                {
                    probe_wait = PROBE_INIT;
                    ts_probe = current + probe_wait;
                }
                else
                {
                    if (KcpUtils.TimeDiff(current, ts_probe) >= 0)
                    {
                        if (probe_wait < PROBE_INIT)
                            probe_wait = PROBE_INIT;
                        probe_wait += probe_wait / 2;
                        if (probe_wait > PROBE_LIMIT)
                            probe_wait = PROBE_LIMIT;
                        ts_probe = current + probe_wait;
                        probe |= ASK_SEND;
                    }
                }
            }
            else
            {
                ts_probe = 0;
                probe_wait = 0;
            }

            if ((probe & ASK_SEND) != 0)
            {
                seg.cmd = CMD_WASK;
                MakeSpace(ref size, OVERHEAD);
                size += seg.Encode(buffer, size);
            }

            if ((probe & ASK_TELL) != 0)
            {
                seg.cmd = CMD_WINS;
                MakeSpace(ref size, OVERHEAD);
                size += seg.Encode(buffer, size);
            }

            probe = 0;

            uint cwnd_ = Math.Min(snd_wnd, rmt_wnd);

            if (!nocwnd) 
                cwnd_ = Math.Min(cwnd, cwnd_);

            while (KcpUtils.TimeDiff(snd_nxt, snd_una + cwnd_) < 0)
            {
                if (snd_queue.Count == 0) 
                    break;

                KcpSegment newseg = snd_queue.Dequeue();

                newseg.conv = conv;
                newseg.cmd = CMD_PUSH;
                newseg.wnd = seg.wnd;
                newseg.ts = current;
                newseg.sn = snd_nxt;

                snd_nxt += 1;

                newseg.una = rcv_nxt;
                newseg.resendts = current;
                newseg.rto = rx_rto;
                newseg.fastack = 0;
                newseg.xmit = 0;

                snd_buf.Add(newseg);
            }

            uint resent = fastresend > 0 ? (uint)fastresend : 0xffffffff;
            uint rtomin = nodelay == 0 ? (uint)rx_rto >> 3 : 0;

            int change = 0;

            foreach (KcpSegment KcpSegment in snd_buf)
            {
                bool needsend = false;

                if (KcpSegment.xmit == 0)
                {
                    needsend = true;
                    KcpSegment.xmit++;
                    KcpSegment.rto = rx_rto;
                    KcpSegment.resendts = current + (uint)KcpSegment.rto + rtomin;
                }
                else if (KcpUtils.TimeDiff(current, KcpSegment.resendts) >= 0)
                {
                    needsend = true;
                    KcpSegment.xmit++;
                    xmit++;
                    if (nodelay == 0)
                    {
                        KcpSegment.rto += Math.Max(KcpSegment.rto, rx_rto);
                    }
                    else
                    {
                        int step = (nodelay < 2) ? KcpSegment.rto : rx_rto;
                        KcpSegment.rto += step / 2;
                    }
                    KcpSegment.resendts = current + (uint)KcpSegment.rto;
                    lost = true;
                }
                else if (KcpSegment.fastack >= resent)
                {
                    if (KcpSegment.xmit <= fastlimit || fastlimit <= 0)
                    {
                        needsend = true;
                        KcpSegment.xmit++;
                        KcpSegment.fastack = 0;
                        KcpSegment.resendts = current + (uint)KcpSegment.rto;
                        change++;
                    }
                }

                if (needsend)
                {
                    KcpSegment.ts = current;
                    KcpSegment.wnd = seg.wnd;
                    KcpSegment.una = rcv_nxt;

                    int need = OVERHEAD + (int)KcpSegment.data.Position;
                    MakeSpace(ref size, need);

                    size += KcpSegment.Encode(buffer, size);

                    if (KcpSegment.data.Position > 0)
                    {
                        Buffer.BlockCopy(KcpSegment.data.GetBuffer(), 0, buffer, size, (int)KcpSegment.data.Position);
                        size += (int)KcpSegment.data.Position;
                    }

                    if (KcpSegment.xmit >= dead_link)
                    {
                        state = -1;
                    }
                }
            }

            KcpSegmentDelete(seg);
            FlushBuffer(size);

            if (change > 0)
            {
                uint inflight = snd_nxt - snd_una;
                ssthresh = inflight / 2;
                if (ssthresh < THRESH_MIN)
                    ssthresh = THRESH_MIN;
                cwnd = ssthresh + resent;
                incr = cwnd * mss;
            }

            if (lost)
            {
                ssthresh = cwnd_ / 2;

                if (ssthresh < THRESH_MIN)
                    ssthresh = THRESH_MIN;

                cwnd = 1;
                incr = mss;
            }

            if (cwnd < 1)
            {
                cwnd = 1;
                incr = mss;
            }
        }

        public void Update(uint currentTimeMilliSeconds)
        {
            current = currentTimeMilliSeconds;

            if (!updated)
            {
                updated = true;
                ts_flush = current;
            }

            int slap = KcpUtils.TimeDiff(current, ts_flush);

            if (slap >= 10000 || slap < -10000)
            {
                ts_flush = current;
                slap = 0;
            }

            if (slap >= 0)
            {
                ts_flush += interval;

                if (current >= ts_flush)           
                {
                    ts_flush = current + interval;
                }

                Flush();
            }
        }

        public uint Check(uint current_)
        {
            uint ts_flush_ = ts_flush;
            int tm_packet = 0x7fffffff;

            if (!updated)
            {
                return current_;
            }

            if (KcpUtils.TimeDiff(current_, ts_flush_) >= 10000 ||
                KcpUtils.TimeDiff(current_, ts_flush_) < -10000)
            {
                ts_flush_ = current_;
            }

            if (KcpUtils.TimeDiff(current_, ts_flush_) >= 0)
            {
                return current_;
            }

            int tm_flush = KcpUtils.TimeDiff(ts_flush_, current_);

            foreach (KcpSegment seg in snd_buf)
            {
                int diff = KcpUtils.TimeDiff(seg.resendts, current_);
                if (diff <= 0)
                {
                    return current_;
                }
                if (diff < tm_packet) tm_packet = diff;
            }

            uint minimal = (uint)(tm_packet < tm_flush ? tm_packet : tm_flush);

            if (minimal >= interval) 
                minimal = interval;

            return current_ + minimal;
        }

        public void SetMtu(uint mtu)
        {
            if (mtu < 50 || mtu < OVERHEAD)
                throw new ArgumentException("MTU must be higher than 50 and higher than OVERHEAD");

            buffer = new byte[(mtu + OVERHEAD) * 3];
            this.mtu = mtu;
            mss = mtu - OVERHEAD;
        }

        public void SetInterval(uint interval)
        {
            // clamp interval between 10 and 5000
            if (interval > 5000) interval = 5000;
            else if (interval < 10) interval = 10;
            this.interval = interval;
        }

        public void SetNoDelay(uint nodelay, uint interval = INTERVAL, int resend = 0, bool nocwnd = false)
        {
            this.nodelay = nodelay;
            if (nodelay != 0)
            {
                rx_minrto = RTO_NDL;
            }
            else
            {
                rx_minrto = RTO_MIN;
            }

            if (interval >= 0)
            {
                if (interval > 5000) 
                    interval = 5000;

                else if (interval < 10) 
                    interval = 10;

                this.interval = interval;
            }

            if (resend >= 0)
            {
                fastresend = resend;
            }

            this.nocwnd = nocwnd;
        }

        public void SetWindowSize(uint sendWindow, uint receiveWindow)
        {
            if (sendWindow > 0)
            {
                snd_wnd = sendWindow;
            }

            if (receiveWindow > 0)
            {
                rcv_wnd = Math.Max(receiveWindow, WND_RCV);
            }
        }
    }
}