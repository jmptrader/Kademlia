using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests2
{
    [TestClass]
    public class BucketListTests
    {
        [TestMethod]
        public void GetCloseContactTest()
        {
            Contact sender = new Contact(null, ID.RandomID);
            Node node = new Node(new Contact(null, ID.RandomID), null);
            List<Contact> contacts = new List<Contact>();
            // Force multiple buckets.
            100.ForEach(() => contacts.Add(new Contact(null, ID.RandomID)));
            contacts.ForEach(c => node.BucketList.AddContact(c));
            ID key = ID.RandomID;            // Pick an ID
            List<Contact> closest = node.FindNode(sender, key).contacts;

            Assert.IsTrue(closest.Count == Constants.K, "Expected K contacts to be returned.");

            // The contacts should be in ascending order with respect to the key.
            var distances = closest.Select(c => c.ID.Value ^ key.Value).ToList();
            var distance = distances[0];

            distances.Skip(1).ForEach(d =>
            {
                Assert.IsTrue(distance < d, "Expected contacts ordered by distance.");
                distance = d;
            });
        }
    }
}
