using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests
{
	[TestClass]
	public class KBucketTests
	{
		/// <summary>
		/// Test that contacts added to a kbucket are in least to most-recently seen order.
		/// </summary>
		[TestMethod]
		public void LeastSeenOrderingTest()
		{
			KBucket kbucket = new KBucket(0);
			ID id = ID.OneID();

			40.ForEach(() =>
			{
				kbucket.HaveContact(new Contact() { NodeID = id }, contact => false);
				id <<= 1;				// id's should be different.
				Thread.Sleep(2);		// need to have some time go by so DateTime's are different.
			});

			Assert.IsTrue(kbucket.Contacts.Count == Constants.K, "Expected k contacts.");

			DateTime last = default(DateTime);
			kbucket.Contacts.ForEach(contact =>
			{
				Assert.IsTrue(last < contact.LastSeen, "Contacts are out of order with regards to last seen.");
				last = contact.LastSeen;
			});
		}
	}
}
