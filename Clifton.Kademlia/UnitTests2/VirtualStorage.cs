using System.Collections.Generic;
using System.Numerics;

using Clifton.Kademlia;

namespace UnitTests2
{
    public class VirtualStorage : IStorage
    {
        protected Dictionary<BigInteger, string> store;

        public VirtualStorage()
        {
            store = new Dictionary<BigInteger, string>();
        }

        public bool TryGetValue(ID key, out string val)
        {
            return store.TryGetValue(key.Value, out val);
        }

        public bool Contains(ID key)
        {
            return store.ContainsKey(key.Value);
        }

        public string Get(ID key)
        {
            return store[key.Value];
        }

        public void Set(ID key, string val)
        {
            store[key.Value] = val;
        }
    }
}
