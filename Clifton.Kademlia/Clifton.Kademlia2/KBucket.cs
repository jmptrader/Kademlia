using System.Collections.Generic;
using System.Numerics;

namespace Clifton.Kademlia
{
    public class KBucket
    {
#if DEBUG       // For unit testing.
        public List<Contact> Contacts { get { return contacts; } }
        public BigInteger Low { get { return low; } }
        public BigInteger High { get { return high; } }
#endif

        protected List<Contact> contacts;
        protected BigInteger low;
        protected BigInteger high;

        /// <summary>
        /// Initializes a k-bucket with the default range of 0 - 2^160
        /// </summary>
        public KBucket()
        {
            contacts = new List<Contact>();
            low = 0;
            high = BigInteger.Pow(new BigInteger(2), 160);
        }

        /// <summary>
        /// Initializes a k-bucket with a specific ID range.
        /// </summary>
        public KBucket(BigInteger low, BigInteger high)
        {
            contacts = new List<Contact>();
            this.low = low;
            this.high = high;
        }

        /// <summary>
        /// Add a contact to the bucket, at the end, as this is the most recently seen contact.
        /// A full bucket throws an exception.
        /// </summary>
        public void AddContact(Contact contact)
        {
            Validate.IsTrue<TooManyContactsException>(contacts.Count < Constants.K, "Bucket is full");
            contacts.Add(contact);
        }
    }
}
