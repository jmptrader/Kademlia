using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace Clifton.Kademlia
{
	public class BucketList
	{
        // used for unit testing, should be read-only list and read-only entries.
        public List<KBucket> Buckets { get { return buckets; } }

		protected List<KBucket> buckets;
        protected ID ourID;

		public BucketList(ID ourID)
		{
            this.ourID = ourID;
			buckets = new List<KBucket>();

            // First kbucket has max range.
            buckets.Add(new KBucket());
		}

        public void AddContact(Contact contact, Func<Contact, bool> discardHead)
        {
            // A node must never put its own node ID into a bucket as a contact.
            if (ourID != contact.NodeID)
            {
                KBucket kbucket = GetKBucket(contact.NodeID);

                if (kbucket.IsBucketFull)
                {
                    if (CanSplit(kbucket))
                    {
                        (KBucket k1, KBucket k2) = kbucket.Split();
                        int idx = GetKBucketIndex(contact.NodeID);
                        buckets[idx] = k1;
                        buckets.Insert(idx + 1, k2);

                        // Recurse until we can't.
                        AddContact(contact, discardHead);
                    }
                    else
                    {
                        kbucket.AddContact(contact, discardHead);
                    }
                }
                else
                {
                    kbucket.AddContact(contact);
                }
            }
        }

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
            return kbucket.HasInRange(ourID) || ((kbucket.Depth() % 5) != 0);
        }

		/// <summary>
		/// Algorithm idea from https://github.com/zencoders/sambatyon/blob/master/Kademlia/Kademlia/BucketList.cs, starting on line 208.
		/// Brute force distance lookup of all known contacts, sorted by distance, then we take at most k (20) of the closest.
		/// </summary>
		/// <param name="toFind">The ID for which we want to find close contacts.</param>
		/// <param name="exclude">The ID to exclude (the requestor's ID)</param>
		public List<Contact> GetCloseContacts(ID toFind, ID exclude)
		{
			var contacts = buckets.
				SelectMany(b => b.Contacts).
				Where(c => c.NodeID != exclude).
				Select(c => new { contact = c, distance = c.NodeID ^ toFind }).
				OrderBy(d => d.distance).
				Take(Constants.K);

			return contacts.Select(c=>c.contact).ToList();
		}

        /// <summary>
        /// For unit testing...
        /// </summary>
        /// <returns>A list of tuples representing the bucket index and the count of contacts in each bucket.</returns>
        public List<(int idx, int count)> GetBucketContactCounts()
        {
            return new List<(int idx, int count)>();
            // TODO: Fix this.
            //return buckets.
            //    Select(b => new { bucket = b, idx = b.Index }).
            //    Where(b => b.bucket.Contacts.Count > 0).
            //    Select(b => (b.idx, b.bucket.Contacts.Count)).ToList();
        }
    }
}
