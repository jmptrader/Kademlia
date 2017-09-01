using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests
{
	[TestClass]
	public class BucketListTests
	{
		// Create k contacts of random ID's and verify that they come back in sorted distances from least distance to greatest.
		// We assume that we will never get two occurrances of the same random ID.
		[TestMethod]
		public void CloseContactTest()
		{
			ID ourID = ID.RandomID();
			ID toFind = ID.RandomID();
			BucketList bucketList = new BucketList(ourID);
			Constants.K.ForEach(() => bucketList.AddContact(new Contact() { NodeID = ID.RandomID() }, (contact) => false));
			List<Contact> contacts = bucketList.GetCloseContacts(toFind, ourID);

			Assert.IsTrue(contacts.Count == Constants.K, "Expected k contacts returned.");

			ID distance = contacts[0].NodeID ^ toFind;

			for (int n = 1; n < Constants.K; n++)
			{
				ID nextDistance = contacts[n].NodeID ^ toFind;
				Assert.IsTrue(distance < nextDistance, "Distances are not sorted in least to greatest order.");
				distance = nextDistance;
			}
		}

        [TestMethod]
        public void NotFullBucketDoesNotSplitTest()
        {
            BucketList bl = new BucketList(ID.MiddleID());
            // any ID for the contact.
            bl.AddContact(new Contact() { NodeID = ID.OneID() }, c => false);
            Assert.IsTrue(bl.Buckets.Count == 1, "Buckets should not have split.");
        }

        /// <summary>
        /// Test whether a split occurs, given 21 contacts with ID's less than 2^80.
        /// </summary>
        [TestMethod]
        public void FullBucketSplitsTest()
        {
            BucketList bl = new BucketList(ID.MiddleID());
            ID id = ID.OneID();
            // add 20 contacts.  
            Constants.K.ForEach(n => bl.AddContact(new Contact() { NodeID = new ID(id.Value + n) }, c => false));
            Assert.IsTrue(bl.Buckets.Count == 1, "Buckets should not have split.");

            // The 21st should result in a split.
            bl.AddContact(new Contact() { NodeID = new ID(id.Value + 20) }, c => false);
            Assert.IsTrue(bl.Buckets.Count == 2, "Expected split.");

            // But because all the contacts where < 2^80, they all went into the first bucket.
            Assert.IsTrue(bl.Buckets[0].Contacts.Count == 20, "Expected 20 contacts in the first bucket.");
            Assert.IsTrue(bl.Buckets[1].Contacts.Count == 0, "Expected 0 contacts in the first bucket.");
        }

        /// <summary>
        /// Test whether 2 splits occur, given 21 contacts evenly distributed in the 2^160 space.
        /// </summary>
        [TestMethod]
        public void DividedBucketSplitsTest()
        {
            BucketList bl = new BucketList(ID.MiddleID());  // any ID will do.

            DistributeContacts(bl);
            Assert.IsTrue(bl.Buckets.Count == 1, "Buckets should not have split.");

            // The 21st should result in a split.
            ID id = ID.OneID();
            bl.AddContact(new Contact() { NodeID = new ID(id.Value + 1) }, c => false);
            Assert.IsTrue(bl.Buckets.Count == 2, "Expected 2 splits.");

            // Verify the buckets contain the correct number of contacts.  Each split should have split contacts in half.
            Assert.IsTrue(bl.Buckets[0].Contacts.Count == 11, "Expected 11 contacts in the first bucket.");
            Assert.IsTrue(bl.Buckets[1].Contacts.Count == 10, "Expected 10 contacts in the first bucket.");
        }

        /// <summary>
        /// Tests that bucket splitting recursion ends.
        /// </summary>
        [TestMethod]
        public void SplitRecursionEndsTest()
        {
            ID z = ID.ZeroID();
            BucketList bl = new BucketList(z);        // our ID is always in range

            // shared bits must all be diferent:
            Constants.K.ForEach(n => bl.AddContact(new Contact() { NodeID = ID.OneID() << n }, c => false));
            Assert.IsTrue(bl.Buckets.Count == 1, "Buckets should not have split.");
            Assert.IsTrue(bl.Buckets[0].Contacts.Count == 20, "Expected 20 contacts.");

            // Add a unique ID (1 and 2 are taken, so use ID = 3)
            bl.AddContact(new Contact() { NodeID = new ID(ID.OneID().Value + 2) }, c => false);
            // At the 142 iteration, the last contact (id = 1 << 19) we added in the starting set can be split into the right bucket.
            Assert.IsTrue(bl.Buckets.Count == 142, "There must be some math to compute this value.");
            Assert.IsTrue(bl.Buckets[0].Contacts.Count == 20, "Expected 20 contacts");
            Assert.IsTrue(bl.Buckets[1].Contacts.Count == 1, "Expected 1 contacts");
            Enumerable.Range(2, 140).ForEach(n => Assert.IsTrue(bl.Buckets[n].Contacts.Count == 0, "Expected 0 contacts."));
        }

        protected void DistributeContacts(BucketList bl)
        {
            BigInteger q = BigInteger.Pow(new BigInteger(2), Constants.ID_LENGTH_BITS - 1);
            ID id = ID.OneID();

            // Add 20 contacts.  The first have should be distributed between 1 - 2^79 space.
            (Constants.K / 2).ForEach(n => bl.AddContact(new Contact()
            {
                NodeID = new ID((id.Value << (n * (Constants.ID_LENGTH_BITS / Constants.K / 2))))
            }, c => false));

            // The second half should be distributed between the 2^79 and 2^80 space.
            (Constants.K / 2).ForEach(n => bl.AddContact(new Contact()
            {
                NodeID = new ID((id.Value << (n * (Constants.ID_LENGTH_BITS / Constants.K / 2))) | q)
            }, c => false));
        }
    }
}
