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
			var distance = ourId ^ contact.NodeID;
			int bucketIdx = distance.GetBucketIndex();
			buckets[bucketIdx].HaveContact(contact, discardHead);
		}
	}
}
