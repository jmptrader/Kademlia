using System.Collections.Generic;

namespace Clifton.Kademlia
{
    public interface IProtocol
    {
        List<Contact> FindNode(ID key);
    }

    public interface IStorage
    {
    }
}
