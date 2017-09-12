using System.Collections.Generic;

namespace Clifton.Kademlia
{
    public class BucketList
    {
#if DEBUG       // Used for unit testing.
        public List<KBucket> Buckets { get { return buckets; } }
#endif

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

        public void AddContact(Contact contact)
        {
            // to be implemented...
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

            return contacts.Select(c => c.contact).ToList();
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
