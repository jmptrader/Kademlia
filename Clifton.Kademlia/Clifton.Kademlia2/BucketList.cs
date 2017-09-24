using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Clifton.Kademlia
{
    public class BucketList
    {
        public List<KBucket> Buckets { get { return buckets; } }

        protected List<KBucket> buckets;
        protected ID ourID;
        protected Contact ourContact;

#if DEBUG       // For unit testing
        public BucketList(ID id, Contact dummyContact)
        {
            ourID = id;
            ourContact = dummyContact;
            buckets = new List<KBucket>();

            // First kbucket has max range.
            buckets.Add(new KBucket());
        }
#endif

        /// <summary>
        /// Initialize the bucket list with our host ID and create a single bucket for the full ID range.
        /// </summary>
        /// <param name="ourID"></param>
        public BucketList(Contact ourContact)
        {
            this.ourContact = ourContact;
            ourID = ourContact.ID;
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

            lock (this)
            {
                contact.Touch();            // Update the LastSeen to now.
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
                        Contact lastSeenContact = kbucket.Contacts.OrderBy(c => c.LastSeen).First();
                        bool stillUp = lastSeenContact.Protocol.Ping(ourContact);

                        if (!stillUp)
                        {
                            kbucket.EvictContact(lastSeenContact);
                            kbucket.AddContact(contact);
                        }
                    }
                }
                else
                {
                    // Bucket isn't full, so just add the contact.
                    kbucket.AddContact(contact);
                }
            }
        }

        public KBucket GetKBucket(ID otherID)
        {
            return buckets[GetKBucketIndex(otherID)];
		}

        public KBucket GetKBucket(BigInteger otherID)
        {
            return buckets[GetKBucketIndex(otherID)];
        }

        /// <summary>
        /// Returns true if the contact, by ID, exists in our bucket list.
        /// </summary>
        public bool ContactExists(Contact sender)
        {
            return Buckets.SelectMany(b => b.Contacts).Any(c => c.ID == sender.ID);
        }

        protected virtual bool CanSplit(KBucket kbucket)
        {
            return kbucket.HasInRange(ourID) || ((kbucket.Depth() % Constants.B) != 0);
        }

        protected int GetKBucketIndex(ID otherID)
		{
			return buckets.FindIndex(b => b.HasInRange(otherID));
		}

        protected int GetKBucketIndex(BigInteger otherID)
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
                Where(c => c.ID != exclude).
                Select(c => new { contact = c, distance = c.ID ^ key }).
                OrderBy(d => d.distance).
                Take(Constants.K);

            return contacts.Select(c => c.contact).ToList();
        }
    }
}
