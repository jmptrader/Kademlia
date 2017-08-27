using System.Collections.Generic;

namespace Clifton.Kademlia.Implementations
{
	public class InMemoryNodeAddress : IAddress
	{
		public Node RecipientNode { get; set; }

		public Contact Ping(Contact sender, IAddress recipient, ID randomID)
		{
			return RecipientNode.Ping(sender);
		}

		public void Store(Contact sender, IAddress recipient, ID randomID, string key, string val)
		{
			RecipientNode.Store(sender, key, val);
		}

		public List<Contact> FindNode(Contact sender, IAddress recipient, ID randomID, ID toFind)
		{
			return RecipientNode.FindNode(sender, toFind);
		}

		public (List<Contact> nodes, string val) FindValue(Contact sender, IAddress recipient, ID randomID, string key)
		{
			return RecipientNode.FindValue(sender, key);
		}
	}
}
