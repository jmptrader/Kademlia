using System;
using System.Collections.Generic;
using System.Numerics;

namespace Clifton.Kademlia
{
    public interface IProtocol
    {
        bool Ping(Contact sender);
        (List<Contact> contacts, bool timeoutError) FindNode(Contact sender, ID key);
        (List<Contact> contacts, string val, bool timeoutError) FindValue(Contact sender, ID key);
        bool Store(Contact sender, ID key, string val, bool isCached = false, int expirationTimeSec = 0);
    }

    public interface IStorage : IEnumerable<BigInteger>
    {
        bool HasValues { get; }
        bool Contains(ID key);
        bool TryGetValue(ID key, out string val);
        string Get(ID key);
        string Get(BigInteger key);
        DateTime GetTimeStamp(BigInteger key);
        void Set(ID key, string value, int expirationTimeSec = 0);
        int GetExpirationTimeSec(BigInteger key);
        void Remove(BigInteger key);

        /// <summary>
        /// Updates the republish timestamp.
        /// </summary>
        void Touch(BigInteger key);
    }
}
