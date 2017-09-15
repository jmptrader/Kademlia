using System.Collections.Generic;

namespace Clifton.Kademlia
{
    public interface IProtocol
    {
        List<Contact> FindNode(Contact sender, ID key);
    }

    public interface IStorage
    {
    }
}
