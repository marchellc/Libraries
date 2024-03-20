using System.Net.NetworkInformation;
using System.Text;
using System;

namespace Networking.Pinging
{
    public struct PingResult
    {
        public string Target;
        public string Data;

        public bool DontFragment;

        public int TimeToLive;
        public int Timeout;

        public int TotalPings;
        public int FailedPings;

        public int PacketLoss;

        public double HighestLatency;
        public double LowestLatency;
        public double AverageLatency;

        public IPStatus[] Results;

        public override string ToString()
        {
            try
            {
                var str =
                $"== PING SUMMARY ==\n" +
                $"Host IP: {Target}\n" +
                $"Data: {Data} ({Encoding.ASCII.GetBytes(Data).Length} bytes)\n" +
                $"Dont Fragment: {DontFragment}\n" +
                $"Time To Live: {TimeToLive}\n" +
                $"Timeout: {Timeout}\n" +
                $"-----------------------\n" +
                $"Total Pings: {TotalPings}\n" +
                $"Failed Pings: {FailedPings}\n" +
                $"-----------------------\n" +
                $"Packet Loss: {PacketLoss}%\n" +
                $"Highest Latency: {HighestLatency} ms\n" +
                $"Lowest Latency: {LowestLatency} ms\n" +
                $"Average Latency: {AverageLatency} ms\n" +
                $"-----------------------\n";

                Results ??= Array.Empty<IPStatus>();

                for (int i = 0; i < Results.Length; i++)
                    str += $"Ping {i + 1}: {Results[i]}\n";

                return str;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}