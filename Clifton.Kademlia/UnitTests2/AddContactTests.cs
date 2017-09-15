using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests2
{
	[TestClass]
	public class AddContactTests
	{
		public const string CRLF = "\r\n";

		[TestMethod]
		public void UniqueIDAddTest()
		{
			BucketList bucketList = new BucketList(ID.RandomIDInKeySpace);
			Constants.K.ForEach(() => bucketList.AddContact(new Contact(null, ID.RandomIDInKeySpace)));
			Assert.IsTrue(bucketList.Buckets.Count == 1, "No split should have taken place.");
			Assert.IsTrue(bucketList.Buckets[0].Contacts.Count == Constants.K, "K contacts should have been added.");									
		}

		[TestMethod]
		public void DuplicateIDTest()
		{
			BucketList bucketList = new BucketList(ID.RandomIDInKeySpace);
			ID id = ID.RandomIDInKeySpace;
			bucketList.AddContact(new Contact(null, id));
			bucketList.AddContact(new Contact(null, id));
			Assert.IsTrue(bucketList.Buckets.Count == 1, "No split should have taken place.");
			Assert.IsTrue(bucketList.Buckets[0].Contacts.Count == 1, "Bucket should have one contact.");
		}

		[TestMethod]
		public void BucketSplitTest()
		{
			BucketList bucketList = new BucketList(ID.RandomIDInKeySpace);
			Constants.K.ForEach(() => bucketList.AddContact(new Contact(null, ID.RandomIDInKeySpace)));
			bucketList.AddContact(new Contact(null, ID.RandomIDInKeySpace));
			Assert.IsTrue(bucketList.Buckets.Count > 1, "Bucket should have split into two or more buckets.");
		}

        /// <summary>
        /// Force a failed add by choosing node ID's that cause depth mod 5 != 0 to be false.
        /// </summary>
        [TestMethod]
        public void ForceFailedAddTest()
        {
            // force host node ID to < 2^159 so the node ID is not in the 2^159 ... 2^160 range
            byte[] hostID = new byte[20];
            hostID[19] = 0x7F;
            BucketList bucketList = new BucketList(new ID(hostID));

            // Also add a contact in this 0 - 2^159 range, arbitrarily something not our host ID.
			// This ensures that only one bucket split will occur after 20 nodes with ID >= 2^159 are added,
			// otherwise, buckets will in the 2^159 ... 2^160 space.
            byte[] id = new byte[20];
            id[0] = 1;
            bucketList.AddContact(new Contact(null, new ID(id)));

            Assert.IsTrue(bucketList.Buckets.Count == 1, "Bucket split should not have occurred.");
            Assert.IsTrue(bucketList.Buckets[0].Contacts.Count == 1, "Expected 1 contact in bucket 0.");

            // make sure contact ID's all have the same 5 bit prefix and are in the 2^159 ... 2^160 - 1 space
            byte[] contactID = new byte[20];
            contactID[19] = 0x80;
            // 1000 xxxx prefix, xxxx starts at 1000 (8)
            // this ensures that all the contacts in a bucket match only the prefix as only the first 5 bits are shared.
			// |----| shared range
			// 1000 1000 ...
			// 1000 1100 ...
			// 1000 1110 ...
            byte shifter = 0x08;
            int pos = 19;

            Constants.K.ForEach(() =>
            {
                contactID[pos] |= shifter;
                bucketList.AddContact(new Contact(null, new ID(contactID)));
                shifter >>= 1;

                if (shifter == 0)
                {
                    shifter = 0x80;
                    --pos;
                }
            });

            Assert.IsTrue(bucketList.Buckets.Count == 2, "Bucket split should have occurred.");
            Assert.IsTrue(bucketList.Buckets[0].Contacts.Count == 1, "Expected 1 contact in bucket 0.");
            Assert.IsTrue(bucketList.Buckets[1].Contacts.Count == 20, "Expected 20 contacts in bucket 1.");

			// This next contact should not split the bucket as depth == 5 and therefore adding the contact will fail.
			// Any unique ID >= 2^159 will do.
			id = new byte[20];
			id[19] = 0x80;
			bucketList.AddContact(new Contact(null, new ID(id)));

			Assert.IsTrue(bucketList.Buckets.Count == 2, "Bucket split should not have occurred.");
			Assert.IsTrue(bucketList.Buckets[0].Contacts.Count == 1, "Expected 1 contact in bucket 0.");
			Assert.IsTrue(bucketList.Buckets[1].Contacts.Count == 20, "Expected 20 contacts in bucket 1.");
		}

		[TestMethod, Ignore]
		public void RandomIDDistributionTest()
		{
			Random rnd = new Random();
			byte[] buffer = new byte[20];
			List<int> contactsAdded = new List<int>();

			100.ForEach(() =>
			{
				rnd.NextBytes(buffer);
				BucketList bucketList = new BucketList(new ID(buffer));

				3200.ForEach(() =>
				{
					rnd.NextBytes(buffer);
					bucketList.AddContact(new Contact(null, new ID(buffer)));
				});

				int contacts = bucketList.Buckets.Sum(b => b.Contacts.Count);
				contactsAdded.Add(contacts);
			});

			Assert.IsTrue(contactsAdded.Average().ApproximatelyEquals(720, 20), "Unexpected distribution.");
			Assert.IsTrue(contactsAdded.Select(n=>(double)n).StdDev().ApproximatelyEquals(10, 2), "Bad distribution");
		}

		[TestMethod, Ignore]
		public void RandomPrefixDistributionTest()
		{
			List<int> contactsAdded = new List<int>();

			100.ForEach(() =>
			{
				BucketList bucketList = new BucketList(ID.RandomIDInKeySpace);
				3200.ForEach(() => bucketList.AddContact(new Contact(null, ID.RandomIDInKeySpace)));
				int contacts = bucketList.Buckets.Sum(b => b.Contacts.Count);
				contactsAdded.Add(contacts);
			});

			double avg = contactsAdded.Average();
			double stdev = contactsAdded.Select(n => (double)n).StdDev();
			Assert.IsTrue(contactsAdded.Average().ApproximatelyEquals(1900, 200), "Unexpected distribution: avg = " + avg);
			Assert.IsTrue(stdev.ApproximatelyEquals(800, 100), "Bad distribution: stdev = " + stdev);
		}

		[TestMethod, Ignore]
		public void DistributionTestForEachPrefix()
		{
			Random rnd = new Random();
			StringBuilder sb = new StringBuilder();
			byte[] buffer = new byte[20];

			160.ForEach((i) =>
			{
				BucketList bucketList = new BucketList(new ID(BigInteger.Pow(new BigInteger(2), i)));

				3200.ForEach(() =>
				{
					rnd.NextBytes(buffer);
					bucketList.AddContact(new Contact(null, new ID(buffer)));
				});

				int contacts = bucketList.Buckets.Sum(b => b.Contacts.Count);
				sb.Append(i + "," + contacts + CRLF);
			});

			File.WriteAllText("prefixTest.txt", sb.ToString());
		}

		[TestMethod, Ignore]
		public void DistributionTestForEachPrefixWithRandomPrefixDistributedContacts()
		{
			StringBuilder sb = new StringBuilder();

			160.ForEach((i) =>
			{
				BucketList bucketList = new BucketList(new ID(BigInteger.Pow(new BigInteger(2), i)));
				3200.ForEach(() => bucketList.AddContact(new Contact(null, ID.RandomIDInKeySpace)));
				int contacts = bucketList.Buckets.Sum(b => b.Contacts.Count);
				sb.Append(i + "," + contacts + CRLF);
			});

			File.WriteAllText("prefixTest.txt", sb.ToString());
		}
	}
}
