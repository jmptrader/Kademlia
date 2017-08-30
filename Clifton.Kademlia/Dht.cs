using System.Collections.Generic;

namespace Clifton.Kademlia
{
	public class Dht
	{
		protected Node node;

		public Dht(Node node)
		{
			this.node = node;
		}

		/// <summary>
		/// Store the value, deriving the key ID from the SHA1 of the key.
		/// </summary>
		public void Store(string key, string val)
		{
			Router router = new Router();
			ID keyID = ID.FromString(key);
			node.Store(node.OurContact, keyID, val);      // we're storing to ourselves as well as k contacts.
			List<Contact> contacts = router.Lookup(keyID, node, NodeLookup).contacts;
			contacts.ForEach(c => c.Address.Store(node.OurContact, c.Address, ID.RandomID(), keyID, val));
		}

		/// <summary>
		/// Caller specifies the keyID.
		/// </summary>
		public void Store(ID keyID, string val)
		{
			Router router = new Router();
			node.Store(node.OurContact, keyID, val);      // we're storing to ourselves as well as k contacts.
			List<Contact> contacts = router.Lookup(keyID, node, NodeLookup).contacts;
			contacts.ForEach(c => c.Address.Store(node.OurContact, c.Address, ID.RandomID(), keyID, val));
		}

		public (bool found, string val) FindValue(string key)
		{
			string ourval;

			// If we have it, return with our value.
			if (node.Storage.TryGetValue(key, out ourval))
			{
				return (true, ourval);
			}

			ID keyID = ID.FromString(key);
			Router router = new Router();

			(List<Contact> contacts, string val) = router.Lookup(keyID, node, ValueLookup);

			return (contacts == null, val);
		}

		public static (List<Contact> contacts, string val) NodeLookup(IAddress ourAddr, Contact us, IAddress theirAddr, ID rID, ID keyID)
		{
			return ourAddr.FindNode(us, theirAddr, rID, keyID);
		}

		public static (List<Contact> contacts, string val) ValueLookup(IAddress ourAddr, Contact us, IAddress theirAddr, ID rID, ID keyID)
		{
			return ourAddr.FindValue(us, theirAddr, rID, keyID);
		}
	}
}
