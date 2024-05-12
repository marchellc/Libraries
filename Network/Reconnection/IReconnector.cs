﻿using Network.Features;

using System;

namespace Network.Reconnection
{
    public interface IReconnector : INetworkFeature
    {
        bool IsAllowed { get; }
        bool IsReconnecting { get; }

        int MaxAttempts { get; }
        int CurAttempts { get; }

        float CurrentDelay { get; }

        DateTime LastTry { get; }
        DateTime NextTry { get; }

        ReconnectionState State { get; }

        void Start();
        void Stop();
    }
}