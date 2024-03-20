using Common.IO.Data;
using Common.Logging;

using Networking.Data;

namespace Networking.Entities
{
    public class NetEntityDataListener<T> : BasicListener<T> 
        where T : IData
    {
        public NetEntityManager EntityManager;
        public LogOutput Log;

        public override void OnRegistered()
        {
            base.OnRegistered();
            EntityManager = Client.Get<NetEntityManager>();
            Log = EntityManager.Log;
        }

        public override void OnUnregistered()
        {
            base.OnUnregistered();
            EntityManager = null;
            Log = null;
        }
    }
}
