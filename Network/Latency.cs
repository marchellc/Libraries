using System;

namespace Network
{
    public struct Latency
    {
        public DateTime SentAt;
        public DateTime ReceivedAt;

        public double Trip;

        public double MaxTrip;
        public double MinTrip;

        public double AverageTrip;
    }
}