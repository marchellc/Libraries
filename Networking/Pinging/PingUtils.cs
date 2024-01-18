using Common.Logging;
using Common.Utilities;

using System;
using System.Net.NetworkInformation;
using System.Text;

namespace Networking.Pinging
{
    public static class PingUtils
    {
        public static LogOutput Log { get; } = new LogOutput("Ping Utils").Setup();

        public static void PingThread(string host, Action<PingResult> callback, int pings = -1, int timeout = -1, string data = null, bool dontFragment = true, int ttl = 128)
            => CodeUtils.OnThread(() => Ping(host, pings, timeout, data, dontFragment, ttl), callback);

        public static PingResult Ping(string host, int pings = 10, int timeout = 500, string data = null, bool dontFragment = true, int ttl = 128)
        {
            data ??= Generator.Instance.GetString(64, true);

            timeout = timeout <= 0 ? 10000 : timeout;
            pings = pings <= 0 ? 10 : pings;

            var dataBytes = Encoding.ASCII.GetBytes(data);

            Log.Verbose($"Pinging {host} with {data.Length} bytes (pings: {pings}; timeout: {timeout}; dont fragment: {dontFragment}; ttl: {ttl})");

            var pingResult = new PingResult()
            {
                AverageLatency = 0,
                HighestLatency = 0,
                LowestLatency = 0,
                FailedPings = 0,
                TotalPings = 0,

                DontFragment = dontFragment,
                Timeout = timeout,
                TimeToLive = ttl,
                Target = host,
                Data = data,
            };

            try
            {
                using (var sender = new Ping())
                {
                    var senderOptions = new PingOptions
                    {
                        DontFragment = dontFragment,
                        Ttl = ttl
                    };

                    var results = new IPStatus[pings];
                    var latencySum = 0;
                    var failed = 0;
                    var highest = 0;
                    var lowest = 0;

                    for (int i = 0; i < pings; i++)
                    {
                        try
                        {
                            var reply = sender.Send(host, timeout, dataBytes, senderOptions);

                            if (reply is null)
                            {
                                failed++;
                                results[i] = IPStatus.Unknown;
                                continue;
                            }

                            results[i] = reply.Status;
                            latencySum += (int)reply.RoundtripTime;

                            if (reply.Status is not IPStatus.Success)
                                failed++;
                            else
                            {
                                if (reply.RoundtripTime > highest)
                                    highest = (int)reply.RoundtripTime;

                                if (reply.RoundtripTime < lowest || lowest == 0)
                                    lowest = (int)reply.RoundtripTime;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);

                            results[i] = IPStatus.Unknown;
                            continue;
                        }
                    }

                    pingResult.AverageLatency = (highest + lowest) / 2;
                    pingResult.PacketLoss = (failed / pings) * 100;
                    pingResult.HighestLatency = highest;
                    pingResult.LowestLatency = lowest;
                    pingResult.FailedPings = failed;
                    pingResult.TotalPings = pings;
                    pingResult.Results = results;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return pingResult;
        }
    }
}