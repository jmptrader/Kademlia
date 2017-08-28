using System;
using System.Collections.Generic;

namespace Clifton.Kademlia
{
	public class BucketList
	{
		protected List<KBucket> buckets;

		public BucketList()
		{
			buckets = new List<KBucket>(Constants.ID_LENGTH_BITS);
			Constants.ID_LENGTH_BITS.ForEach(() => buckets.Add(new KBucket()));
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
		/// </summary>
		/// <param name="toFind">The ID for which we want to find close contacts.</param>
		/// <param name="exclude">The ID to exclude (the requestor's ID)</param>
		public List<Contact> GetCloseContacts(ID toFind, ID exclude)
		{
			List<Contact> contacts = new List<Contact>();

			return contacts;
		}
	}
}
