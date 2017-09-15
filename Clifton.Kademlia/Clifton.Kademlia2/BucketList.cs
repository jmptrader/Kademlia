using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
    public class BucketList
    {
        public List<KBucket> Buckets { get { return buckets; } }

        protected List<KBucket> buckets;
        protected ID ourID;

        /// <summary>
        /// Initialize the bucket list with our host ID and create a single bucket for the full ID range.
        /// </summary>
        /// <param name="ourID"></param>
        public BucketList(ID ourID)
        {
            this.ourID = ourID;
            buckets = new List<KBucket>();

            // First kbucket has max range.
            buckets.Add(new KBucket());
        }

		/// <summary>
		/// Add a contact if possible, based on the algorithm described
		/// in sections 2.2, 2.4 and 4.2
		/// </summary>
		public void AddContact(Contact contact)
		{
			Validate.IsFalse<OurNodeCannotBeAContactException>(ourID == contact.ID, "Cannot add ourselves as a contact!");

			contact.Touch();			// Update the LastSeen to now.
			KBucket kbucket = GetKBucket(contact.ID);

			if (kbucket.Contains(contact.ID))
			{
				// Replace the existing contact, updating the network info and LastSeen timestamp.
				kbucket.ReplaceContact(contact);
			}
			else if (kbucket.IsBucketFull)
			{
				if (CanSplit(kbucket))
				{
					// Split the bucket and try again.
					(KBucket k1, KBucket k2) = kbucket.Split();
					int idx = GetKBucketIndex(contact.ID);
					buckets[idx] = k1;
					buckets.Insert(idx + 1, k2);
					AddContact(contact);
				}
				else
				{
					// TODO: Ping the oldest contact to see if it's still 
					// around and replace it if not.
				}
			}
			else
			{
				// Bucket isn't full, so just add the contact.
				kbucket.AddContact(contact);
			}
		}

		protected virtual bool CanSplit(KBucket kbucket)
		{
			return kbucket.HasInRange(ourID) || ((kbucket.Depth() % Constants.B) != 0);
		}

#if DEBUG
        public KBucket GetKBucket(ID otherID)
#else
        protected KBucket GetKBucket(ID otherID)
#endif
        {
            return buckets[buckets.FindIndex(b => b.HasInRange(otherID))];
		}

		protected int GetKBucketIndex(ID otherID)
		{
			return buckets.FindIndex(b => b.HasInRange(otherID));
		}

        /// <summary>
        /// Brute force distance lookup of all known contacts, sorted by distance, then we take at most k (20) of the closest.
        /// </summary>
        /// <param name="toFind">The ID for which we want to find close contacts.</param>
        /// <param name="exclude">The ID to exclude (the requestor's ID)</param>
        public List<Contact> GetCloseContacts(ID key, ID exclude)
        {
            var contacts = buckets.
                SelectMany(b => b.Contacts).
                Where(c => c.ID.Value != exclude.Value).
                Select(c => new { contact = c, distance = c.ID.Value ^ key.Value }).
                OrderBy(d => d.distance).
                Take(Constants.K);

            return contacts.Select(c => c.contact).ToList();
        }

        /*
        public int GetKBucketIndex(ID otherID)
        {
            return buckets.FindIndex(b => b.HasInRange(otherID));
        }

        public KBucket GetKBucket(ID otherID)
        {
            return buckets[buckets.FindIndex(b => b.HasInRange(otherID))];
        }

        protected bool CanSplit(KBucket kbucket)
        {
            return kbucket.High - kbucket.Low >= 2;
            // return kbucket.HasInRange(ourID) || ((kbucket.Depth() % 5) != 0);
        }

        /// <summary>
        /// For unit testing...
        /// </summary>
        /// <returns>A list of tuples representing the bucket index and the count of contacts in each bucket.</returns>
        public List<(int idx, int count)> GetBucketContactCounts()
        {
            List<(int idx, int count)> contactCounts = new List<(int idx, int count)>();

            buckets.Where(b => b.Contacts.Count > 0).ForEachWithIndex((b, n) => contactCounts.Add((n, b.Contacts.Count)));

            return contactCounts;
        }
        */
    }
}
