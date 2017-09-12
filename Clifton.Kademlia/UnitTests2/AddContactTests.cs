using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests2
{
	[TestClass]
	public class AddContactTests
	{
		[TestMethod]
		public void UniqueIDAddTest()
		{
			BucketList bucketList = new BucketList(ID.RandomID);
			Constants.K.ForEach(() => bucketList.AddContact(new Contact(null, ID.RandomID)));
			Assert.IsTrue(bucketList.Buckets.Count == 1, "No split should have taken place.");
			Assert.IsTrue(bucketList.Buckets[0].Contacts.Count == Constants.K, "K contacts should have been added.");									
		}

		[TestMethod]
		public void DuplicateIDTest()
		{
			BucketList bucketList = new BucketList(ID.RandomID);
			ID id = ID.RandomID;
			bucketList.AddContact(new Contact(null, id));
			bucketList.AddContact(new Contact(null, id));
			Assert.IsTrue(bucketList.Buckets.Count == 1, "No split should have taken place.");
			Assert.IsTrue(bucketList.Buckets[0].Contacts.Count == 1, "Bucket should have one contact.");
		}

		[TestMethod]
		public void BucketSplitTest()
		{
			BucketList bucketList = new BucketList(ID.RandomID);
			Constants.K.ForEach(() => bucketList.AddContact(new Contact(null, ID.RandomID)));
			bucketList.AddContact(new Contact(null, ID.RandomID));
			Assert.IsTrue(bucketList.Buckets.Count > 1, "Bucket should have split into two or more buckets.");
		}

		[TestMethod]
		public void RandomPrefixDistributionTest()
		{
			BucketList bucketList = new BucketList(ID.RandomID);
			List<int> contactsAdded = new List<int>();

			100.ForEach(() =>
			{
				3200.ForEach(() => bucketList.AddContact(new Contact(null, ID.RandomID)));
				int contacts = bucketList.Buckets.Sum(b => b.Contacts.Count);
				contactsAdded.Add(contacts);
			});

			Assert.IsTrue(contactsAdded.Average().ApproximatelyEquals(3300, 100), "Unexpected distribution.");
			Assert.IsTrue(contactsAdded.Select(n => (double)n).StdDev().ApproximatelyEquals(630, 60), "Bad distribution");
		}

		[TestMethod]
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

		[TestMethod]
		public void DistributionTest3()
		{
			SHA1Managed sha1 = new SHA1Managed();
			Random rnd = new Random();
			byte[] buffer = new byte[20];
			rnd.NextBytes(buffer);
			BucketList bucketList = new BucketList(new ID(sha1.ComputeHash(buffer)));

			3200.ForEach(() =>
			{
				rnd.NextBytes(buffer);
				bucketList.AddContact(new Contact(null, new ID(sha1.ComputeHash(buffer))));
			});

			int contacts = bucketList.Buckets.Sum(b => b.Contacts.Count);
		}
	}
}
