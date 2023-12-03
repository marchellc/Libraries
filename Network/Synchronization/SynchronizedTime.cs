using Common.Extensions;

using System;

namespace Network.Synchronization
{
    public class SynchronizedTime : SynchronizedDelegatedValue<TimeSpan>
    {
        public SynchronizedTime() : base(
            br => br.ReadTime(),
            (bw, time) => bw.WriteTime(time))
        {
        }
    }
}
