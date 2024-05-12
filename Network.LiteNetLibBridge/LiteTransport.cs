using Common.IO.Collections;
using Common.IO.Data;

using LiteNetLib;
using LiteNetLib.Utils;

using Network.Controllers.Server;
using Network.Targets.Ip;

using System;
using System.Collections.Generic;

namespace Network.LiteNetLibBridge
{
    public static class LiteTransport
    {
        public static readonly LockedDictionary<NetPeer, ServerPeer> Peering = new LockedDictionary<NetPeer, ServerPeer>();
        public static readonly LockedDictionary<Guid, NetPeer> PeerIds = new LockedDictionary<Guid, NetPeer>();

        public static void DiscardAll()
        {
            Peering.Clear();
            PeerIds.Clear();
        }

        public static NetPeer GetPeer(Guid guid)
            => PeerIds.TryGetValue(guid, out var peer) ? peer : null;

        public static ServerPeer GetPeer(NetPeer netPeer, ServerBridge serverBridge)
        {
            if (Peering.TryGetValue(netPeer, out var serverPeer))
                return serverPeer;

            var id = Guid.NewGuid();
            var target = new IPTarget(netPeer.Address, netPeer.Port);

            serverPeer = new ServerPeer(id, serverBridge, target, serverBridge.Controller.Events);

            Peering[netPeer] = serverPeer;
            PeerIds[id] = netPeer;

            return serverPeer;
        }

        public static void Discard(NetPeer peer)
        {
            if (Peering.TryGetValue(peer, out var serverPeer))
            {
                PeerIds.Remove(serverPeer.Id);
                Peering.Remove(peer);
            }
        }

        public static NetDataWriter Write(IEnumerable<object> objects)
        {
            var dataWriter = DataWriter.Get();

            dataWriter.WriteEnumerable(objects);

            var bytes = dataWriter.Data;

            dataWriter.Return();
            dataWriter = null;

            var writer = new NetDataWriter();

            writer.Put(bytes.Length);
            writer.Put(bytes);

            return writer;
        }

        public static object[] Read(NetDataReader reader)
        {
            var bytes = reader.GetInt();
            var bytesArray = new byte[bytes];

            reader.GetBytes(bytesArray, bytes);

            var dataReader = DataReader.Get(bytesArray);
            var dataObjects = dataReader.ReadArray<object>();

            dataReader.Return();
            dataReader = null;

            return dataObjects;
        }
    }
}