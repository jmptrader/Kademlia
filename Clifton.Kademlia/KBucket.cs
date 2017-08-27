using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
	public class KBucket
	{
		public bool Exists(ID id)
		{
			return contacts.Any(c => c.NodeID == id);
		}

		protected List<Contact> contacts;

		public KBucket()
		{
			contacts = new List<Contact>(Constants.K);
		}

		public void HaveContact(Contact contact)
		{
			if (Exists(contact.NodeID))
			{
				contacts.MoveToTail(contact, c => c.NodeID == contact.NodeID);
			}
			else
			{
				contacts.AddMaximum(contact, Constants.K);
			}
		}
	}
}
