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
        protected Dictionary<BigInteger, DateTime> republishTimestamps;

        public VirtualStorage()
        {
            store = new Dictionary<BigInteger, string>();
            republishTimestamps = new Dictionary<BigInteger, DateTime>();
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

        public DateTime GetTimeStamp(BigInteger key)
        {
            return republishTimestamps[key];
        }

		/// <summary>
		/// Updates the republish timestamp.
		/// </summary>
		public void Touch(BigInteger key)
        {
            republishTimestamps[key] = DateTime.Now;
        }

        public void Set(ID key, string val)
        {
            store[key.Value] = val;
            Touch(key.Value);
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
