using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
    public class Router
    {
		public List<Contact> NodeLookup(ID id, Node ourNode)
		{
			BucketList bucketList = ourNode.BucketList;

			// Take alpha close contacts, excluding ourselves (we should never be in our own bucket list anyways.)
			// The first alpha contacts selected are used to create a shortlist for the search.
			List<Contact> shortList = bucketList.GetCloseContacts(id, ourNode.OurContact.NodeID).Take(Constants.ALPHA).ToList();

			// Some work to do?
			if (shortList.Count > 0)
			{
				List<Contact> successfulContacts = new List<Contact>();
				bool hasNewCloseContact = false;
				Contact closestNode;
				ID distance;

				do
				{
					distance = shortList[0].NodeID ^ id;
					closestNode = shortList[0];
					// From the shortlist it selects another alpha contacts.
					shortList = LookupCloserContacts(id, ourNode, shortList.Take(Constants.ALPHA).ToList(), successfulContacts);
					// Once again a FIND_* RPC is sent to each in parallel.
					// Each such parallel search updates closestNode, the closest node seen so far.
					hasNewCloseContact = (shortList[0].NodeID ^ id) < distance;

				// The sequence of parallel searches is continued until either no node in the sets returned is closer 
				// than the closest node already seen or the initiating node has accumulated k probed 
				// and known to be active contacts.
				} while (hasNewCloseContact && shortList.Count < Constants.K);
			}

			return shortList;
		}

		protected List<Contact> LookupCloserContacts(ID id, Node ourNode, List<Contact> shortList, List<Contact> successfulContacts)
		{
			List<Contact> unsucessfulContacts = new List<Contact>();

			// The known closest node is the first entry.
			List<Contact> newContacts = new List<Contact>();

			// The node then sends parallel, asynchronous FIND_* RPCs to the alpha contacts in the shortlist. 
			// Each contact, if it is live, should normally return k triples. 
			// If any of the alpha contacts fails to reply, it is removed from the shortlist, at least temporarily.
			shortList.ForEach(c =>
			{
				List<Contact> targetContacts = ourNode.OurContact.Address.FindNode(ourNode.OurContact, c.Address, ID.RandomID(), id);

				// Let's assume that failure to contact a node results in a null.
				if (targetContacts != null)
				{
					successfulContacts.Add(c);

					// Add the nodes it returns to the new contacts list.
					newContacts.AddRangeDistinct(targetContacts, (a, b) => a.NodeID == b.NodeID);
				}
				else
				{
					unsucessfulContacts.Add(c);
				}
			});

			// Each such parallel search updates closestNode, the closest node seen so far.

			// The node then fills the shortlist with contacts from the replies received. 
			// These are those closest to the target. 
			// From the shortlist it selects another alpha contacts. 
			// The only condition for this selection is that they have not already been contacted. 
			shortList.RemoveRange(unsucessfulContacts);	// These are our references
			newContacts.RemoveRange(successfulContacts, (a, b) => a.NodeID == b.NodeID); // Not our references, equality operator necessary
			shortList.AddRangeDistinct(newContacts, (a, b) => a.NodeID == b.NodeID);
			shortList = shortList.OrderBy(c => c.NodeID ^ id).ToList();         // sort by distance!

			return shortList;
		}
    }
}
