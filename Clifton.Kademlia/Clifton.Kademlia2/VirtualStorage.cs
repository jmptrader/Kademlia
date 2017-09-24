using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace Clifton.Kademlia
{
    public class VirtualStorage : IStorage
    {
        public bool HasValues { get { return store.Count > 0; } }

        protected ConcurrentDictionary<BigInteger, string> store;
        protected ConcurrentDictionary<BigInteger, DateTime> republishTimestamps;
        protected ConcurrentDictionary<BigInteger, int> expirationTimes;

        public VirtualStorage()
        {
            store = new ConcurrentDictionary<BigInteger, string>();
            republishTimestamps = new ConcurrentDictionary<BigInteger, DateTime>();
            expirationTimes = new ConcurrentDictionary<BigInteger, int>();
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

        public int GetExpirationTimeSec(BigInteger key)
        {
            return expirationTimes[key];
        }

		/// <summary>
		/// Updates the republish timestamp.
		/// </summary>
		public void Touch(BigInteger key)
        {
            republishTimestamps[key] = DateTime.Now;
        }

        public void Set(ID key, string val, int expirationTime)
        {
            store[key.Value] = val;
            expirationTimes[key.Value] = expirationTime;
            Touch(key.Value);
        }

        public void Remove(BigInteger key)
        {
            store.TryRemove(key, out _);
            republishTimestamps.TryRemove(key, out _);
            expirationTimes.TryRemove(key, out _);
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
