using System;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
	public class KBucket
	{
		public List<Contact> Contacts { get { return contacts; } }

		public bool Exists(ID id)
		{
			return contacts.Any(c => c.NodeID == id);
		}

		protected List<Contact> contacts;

		public KBucket()
		{
			contacts = new List<Contact>(Constants.K);
		}

		public void HaveContact(Contact contact, Func<Contact, bool> discardHead)
		{
			contact.Touch();

			// If contact exists, promote it it to the tail.
			if (Exists(contact.NodeID))
			{
				contacts.MoveToTail(contact, c => c.NodeID == contact.NodeID);
			}
			else
			{
				// When contact doesn't exist, if there's room to add it, just do so.
				if (contacts.Count < Constants.K)
				{
					contacts.Add(contact);
				}
				else if (discardHead(contacts[0]))
				{
					// Otherwise, if the least recently seen node doesn't respond to a ping, discard it and
					// replace it with our new contact.
					contacts.AddMaximum(contact, Constants.K);
				}
				// Otherwise we discard the new contact, as we don't know anything about how reliable it is.
			}
		}
	}
}
