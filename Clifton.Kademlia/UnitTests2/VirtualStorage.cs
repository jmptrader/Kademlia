using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

using Clifton.Kademlia;

namespace UnitTests2
{
    public class VirtualStorage : IStorage
    {
        public bool HasValues { get { return store.Count > 0; } }

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

        public string Get(BigInteger key)
        {
            return store[key];
        }

        public void Set(ID key, string val)
        {
            store[key.Value] = val;
        }

        public IEnumerator<BigInteger> GetEnumerator()
        {
            return store.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return store.Keys.GetEnumerator();
        }
    }
}
