using System.Collections.Generic;
using System.Numerics;

namespace Clifton.Kademlia
{
    public interface IProtocol
    {
        bool Ping(Contact sender);
        List<Contact> FindNode(Contact sender, ID key);
        (List<Contact> contacts, string val) FindValue(Contact sender, ID key);
        void Store(Contact sender, ID key, string val);
    }

    public interface IStorage : IEnumerable<BigInteger>
    {
        bool HasValues { get; }
        bool Contains(ID key);
        bool TryGetValue(ID key, out string val);
        string Get(ID key);
        string Get(BigInteger key);
        void Set(ID key, string value);
    }
}
