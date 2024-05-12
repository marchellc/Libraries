using Common.Values;

using Network.Features;

using System;

namespace Network.Latency
{
    public interface ILatencyMeter : INetworkFeature
    {
        DateTime TimeSent { get; }
        DateTime TimeReceived { get; }

        LatencySide Side { get; set; }

        StatisticValue<int> Latency { get; }

        int Interval { get; set; }

        void Measure(Action callback);
    }
}