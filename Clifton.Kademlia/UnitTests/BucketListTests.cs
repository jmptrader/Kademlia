using System;
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
			BucketList bucketList = new BucketList();
			Constants.K.ForEach(() => bucketList.HaveContact(ourID, new Contact() { NodeID = ID.RandomID() }, (contact) => false));
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
	}
}
