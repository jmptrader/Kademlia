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
            List<Contact> allContacts = new List<Contact>(shortList);

			// Some work to do?
			if (shortList.Count > 0)
			{
				List<Contact> successfulContacts = new List<Contact>();
				bool hasNewCloseContact = false;
				ID distance;
                distance = shortList[0].NodeID ^ id;
                List<Contact> workingShortList = new List<Contact>(shortList);

                do
                {
                    List<Contact> newContacts = LookupCloserContacts(id, ourNode, workingShortList, successfulContacts);
                    allContacts.AddRangeDistinct(newContacts, (a, b) => a.NodeID == b.NodeID);

                    // The node then fills the shortlist with contacts from the replies received. 
                    shortList.AddRangeDistinct(newContacts, (a, b) => a.NodeID == b.NodeID);
                    shortList = shortList.OrderBy(c => c.NodeID ^ id).ToList();

                    // From the shortlist it selects another alpha contacts.
                    workingShortList = new List<Contact>(shortList);

                    // The only condition for this selection is that they have not already been contacted.
                    workingShortList.RemoveRange(successfulContacts, (a, b) => a.NodeID == b.NodeID);
                    workingShortList = workingShortList.OrderBy(c => c.NodeID ^ id).Take(Constants.ALPHA).ToList();         // sort by distance!

                    // Each such parallel search updates closestNode, the closest node seen so far.
                    // TODO: Make this parallel!
                    hasNewCloseContact = (shortList[0].NodeID ^ id) < distance;

                    if (hasNewCloseContact)
                    {
                        distance = shortList[0].NodeID ^ id;
                    }
                    else
                    {
                        // If a cycle doesn't find a closer node, if closestNode is unchanged, 
                        // then the initiating node sends a FIND_* RPC to each of the k closest nodes that it has not already queried.
                        workingShortList = new List<Contact>(allContacts);
                        workingShortList.RemoveRange(successfulContacts);
                        workingShortList = workingShortList.Take(Constants.K).ToList();
                    }

                    // Once again a FIND_* RPC is sent to each in parallel.

                    // The sequence of parallel searches is continued until either no node in the sets returned is closer 
                    // than the closest node already seen or the initiating node has accumulated k probed 
                    // and known to be active contacts.
                } while ((!hasNewCloseContact || shortList.Count < Constants.K) && workingShortList.Count > 0);
			}

            // Return at most k contacts.
            return shortList.Take(Constants.K).ToList();
		}

		protected List<Contact> LookupCloserContacts(ID id, Node ourNode, List<Contact> workingShortList, List<Contact> successfulContacts)
		{
			List<Contact> unsucessfulContacts = new List<Contact>();

			// The known closest node is the first entry.
			List<Contact> newContacts = new List<Contact>();

			// The node then sends parallel, asynchronous FIND_* RPCs to the alpha contacts in the shortlist. 
			// Each contact, if it is live, should normally return k triples. 
			// If any of the alpha contacts fails to reply, it is removed from the shortlist, at least temporarily.
			workingShortList.ForEach(c =>
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

            // If any of the alpha contacts fails to reply, it is removed from the shortlist, at least temporarily.
            workingShortList.RemoveRange(unsucessfulContacts); // These are our references

            return newContacts;
		}
    }
}
