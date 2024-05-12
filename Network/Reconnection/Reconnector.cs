using Network.Controllers;
using Network.Events;
using Network.Features;

using System;
using System.Threading;

namespace Network.Reconnection
{
    public class Reconnector : NetworkFeatureBase, IReconnector
    {
        private volatile int m_MaxAttempts = 10;
        private volatile int m_CurAttempts;

        private volatile float m_Delay;

        private volatile bool m_Allowed;
        private volatile bool m_Cancelled;

        private volatile ReconnectionState m_State;

        private DateTime m_LastTime;
        private DateTime m_NextTry;

        private Thread m_Thread;

        public bool IsAllowed => m_Allowed;
        public bool IsReconnecting => m_State == ReconnectionState.Reconnecting;

        public int MaxAttempts => m_MaxAttempts;
        public int CurAttempts => m_CurAttempts;

        public float CurrentDelay => m_Delay;

        public DateTime LastTry => m_LastTime;
        public DateTime NextTry => m_NextTry;

        public ReconnectionState State => m_State;

        public void Start()
        {
            if (m_Thread != null)
                Stop();

            Log.Info("Starting the reconnection sequence ..");

            m_Thread = new Thread(Advance);
            m_Thread.Start();
        }

        public void Stop()
        {
            m_Allowed = false;
            m_Delay = 1500f;
            m_State = ReconnectionState.Connected;
            m_Cancelled = true;
            m_Thread = null;
        }

        public override void Install(INetworkEvents networkEvents)
        {
            base.Install(networkEvents);

            networkEvents.OnDisconnected += OnDisconnected;
            networkEvents.OnConnected += OnConnected;
        }

        public override void Uninstall(INetworkEvents networkEvents)
        {
            base.Uninstall(networkEvents);

            networkEvents.OnDisconnected -= OnDisconnected;
            networkEvents.OnConnected -= OnConnected;
        }

        private void OnConnected(Peers.INetworkPeer obj)
            => Stop();

        private void OnDisconnected(Peers.INetworkPeer arg1, DisconnectReason arg2)
        {
            if (arg1 != null || !arg2.ShouldReconnect)
                return;

            Start();
        }

        private void Advance()
        {
            Log.Info("Reconnection thread started.");

            m_Allowed = true;
            m_Cancelled = false;

            m_NextTry = DateTime.Now;
            m_LastTime = DateTime.Now;

            m_State = ReconnectionState.Reconnecting;
            m_CurAttempts = 0;
            m_Delay = 1500f;

            while (!m_Cancelled)
            {
                if (!m_Allowed)
                    continue;

                if (m_State == ReconnectionState.Cooldown)
                {
                    if ((DateTime.Now - m_LastTime).TotalMilliseconds >= m_Delay)
                    {
                        m_State = ReconnectionState.Reconnecting;
                        Log.Info("Cooldown expired, attempting reconnection.");
                    }
                }
                else if (m_State == ReconnectionState.ColldownFailure)
                {
                    if (DateTime.Now >= NextTry)
                    {
                        m_Delay += 1000f;

                        if (m_Delay >= 60000f)
                        {
                            Stop();
                            m_State = ReconnectionState.ColldownFailure;
                            Log.Warn($"The reconnection delay reached a minute - disabling reconnection.");
                        }
                        else
                        {
                            m_State = ReconnectionState.Reconnecting;
                            Log.Info("Cooldown expired, attempting reconnection.");
                        }
                    }
                }
                else
                {
                    if (m_State != ReconnectionState.Reconnecting || !((DateTime.Now - m_LastTime).TotalMilliseconds >= 2500))
                        continue;

                    if (m_CurAttempts >= m_MaxAttempts)
                    {
                        m_State = ReconnectionState.ColldownFailure;
                        m_NextTry = DateTime.Now.AddSeconds(60);
                        m_LastTime = DateTime.Now;
                        m_CurAttempts = 0;

                        Log.Warn("Reached the reconnection attempt limit! Waiting a minute before retrying ..");
                        continue;
                    }

                    m_CurAttempts++;
                    m_LastTime = DateTime.Now;
                    m_State = ReconnectionState.Reconnecting;

                    Log.Info("Reconnecting ..");

                    try { (Controller as INetworkController).Connect(); } catch { }
                }
            }
        }
    }
}