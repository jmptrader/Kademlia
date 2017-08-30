using System;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
	public class BucketList
	{
		protected List<KBucket> buckets;

		public BucketList()
		{
			buckets = new List<KBucket>(Constants.ID_LENGTH_BITS);

            Constants.ID_LENGTH_BITS.ForEach((n) => buckets.Add(new KBucket(n)));
		}

		public void HaveContact(ID ourId, Contact contact, Func<Contact, bool> discardHead)
		{
			// A node must never put its own node ID into a bucket as a contact.
			if (ourId != contact.NodeID)
			{
				var distance = ourId ^ contact.NodeID;
				int bucketIdx = distance.GetBucketIndex();
				buckets[bucketIdx].HaveContact(contact, discardHead);
			}
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
        /// <returns></returns>
        public List<(int idx, int count)> GetBucketContactCounts()
        {
            return buckets.
                Select(b => new { bucket = b, idx = b.Index }).
                Where(b => b.bucket.Contacts.Count > 0).
                Select(b => (b.idx, b.bucket.Contacts.Count)).ToList();
        }
    }
}
