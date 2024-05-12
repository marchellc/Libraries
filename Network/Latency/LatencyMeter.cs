using Common.Extensions;
using Common.Values;

using Network.Controllers;
using Network.Features;
using Network.Latency.Messages;
using Network.Peers;
using Network.Requests;

using System;
using System.Timers;

namespace Network.Latency
{
    public class LatencyMeter : NetworkFeatureBase, ILatencyMeter
    {
        private readonly StatisticValue<int> m_Stats;
        private readonly LatencyData m_Data;

        private Timer m_Timer;

        private int m_Interval = 500;

        public DateTime TimeSent => m_Data.Sent;
        public DateTime TimeReceived => m_Data.Received;

        public LatencySide Side { get; set; } = LatencySide.Server;

        public StatisticValue<int> Latency => m_Stats;

        public int Interval { get => m_Interval; set => m_Interval = value; }

        public void Measure(Action callback)
        {
            Controller.ExecuteFeature<IRequestManager>(req =>
            {
                req.Send(LatencyRequestMessage.Instance, res =>
                {
                    m_Data.Sent = res.SentAt;
                    m_Data.Received = res.ReceivedAt;
                    m_Data.Trip = (res.ReceivedAt - res.SentAt).Milliseconds;

                    m_Stats.Value = m_Data.Trip;

                    callback.Call();
                });
            });
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            Controller.ExecuteFeature<IRequestManager>(req => req.RegisterHandler<LatencyRequestMessage>(Respond));

            if (ValidateSide() && m_Interval > 0)
            {
                m_Timer = new Timer(m_Interval);
                m_Timer.Elapsed += Update;
                m_Timer.Enabled = true;
                m_Timer.Start();
            }
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            if (m_Timer != null)
            {
                m_Timer.Elapsed -= Update;
                m_Timer.Enabled = false;
                m_Timer.Stop();
                m_Timer.Dispose();
                m_Timer = null;
            }

            Controller.ExecuteFeature<IRequestManager>(req => req.RemoveHandler<LatencyRequestMessage>(Respond));
        }

        private bool ValidateSide()
        {
            if (Side == LatencySide.Both)
                return true;

            if (Side == LatencySide.Client)
                return Controller is INetworkController controller && controller.Type == ControllerType.Client;

            if (Side == LatencySide.Server)
                return Controller is INetworkPeer;

            return false;
        }

        private void Update(object _, ElapsedEventArgs ev)
        {
            if (Interval > 0)
                Measure(null);
        }

        private void Respond(IRequest request, LatencyRequestMessage latencyRequestMessage)
            => request.RespondSuccess(LatencyResponseMessage.Instance);
    }
}