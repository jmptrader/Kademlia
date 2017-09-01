using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace Clifton.Kademlia
{
	public class KBucket
	{
        public BigInteger Low { get; protected set; }
        public BigInteger High { get; protected set; }
		public List<Contact> Contacts { get { return contacts; } }
        public bool IsBucketFull { get { return contacts.Count == Constants.K; } }

		protected List<Contact> contacts;

        public KBucket()
        {
            Low = new BigInteger(0);
            High = BigInteger.Pow(new BigInteger(2), Constants.ID_LENGTH_BITS);
            contacts = new List<Contact>();
        }

        public KBucket(BigInteger low, BigInteger high)
		{
            Low = low;
            High = high;
			contacts = new List<Contact>();
		}

        public bool Exists(ID id)
        {
            return contacts.Any(c => c.NodeID == id);
        }

        /// <summary>
        /// True if value within [Low, High)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasInRange(ID id)
        {
            return Low <= id.Value && id.Value < High;
        }

        /// <summary>
        /// Returns number of bits that are in common across all contacts.
        /// If there are no contacts, or no shared bits, the return is 0.
        /// </summary>
        public int Depth()
        {
            bool[] bits = new bool[0];

            if (contacts.Count > 0)
            {
                // Start with the first contact.
                bits = contacts[0].NodeID.Bytes.Bits().Reverse().ToArray();

                contacts.Skip(1).ForEach(c => bits = SharedBits(bits, c.NodeID));
            }

            return bits.Length;
        }

        /// <summary>
        /// Add the contact, knowing that the bucket has room.
        /// </summary>
        public void AddContact(Contact contact)
        {
            Validate.IsTrue(contacts.Count < Constants.K, "KBucket is full!");
            contact.Touch();
            contacts.Add(contact);
        }

        /// <summary>
        /// Add the contact.
        /// </summary>
        public void AddContact(Contact contact, Func<Contact, bool> discardHead)
		{
            contact.Touch();

			// If contact exists, promote it it to the tail.
			if (Exists(contact.NodeID))
			{
				contacts.MoveToTail(contact, c => c.NodeID == contact.NodeID);
			}
			else
			{
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

        /// <summary>
        /// Splits the kbucket into returning two new kbuckets filled with contacts separated by the new midpoint
        /// </summary>
        public (KBucket, KBucket) Split()
        {
            BigInteger midpoint = (Low + High) / 2;
            KBucket k1 = new KBucket(Low, midpoint);
            KBucket k2 = new KBucket(midpoint + 1, High);

            Contacts.ForEach(c =>
            {
                // <, because the High value is exclusive in the HasInRange test.
                // If value == midpoint, HasInRange would fail
                KBucket k = c.NodeID.Value < midpoint ? k1 : k2;
                k.AddContact(c);
            });

            return (k1, k2);
        }

        /// <summary>
        /// Returns a new bit array of just the shared bits.
        /// </summary>
        protected bool[] SharedBits(bool[] bits, ID id)
        {
            bool[] idbits = id.Bytes.Bits().Reverse().ToArray();
            int n = 0;
            List<bool> sharedBits = new List<bool>();

            while (n < bits.Length && bits[n] == idbits[n])
            {
                sharedBits.Add(bits[n]);
                ++n;
            }

            return sharedBits.ToArray();
        }
    }
}
