using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;
using Clifton.Kademlia.Implementations;

namespace UnitTests
{
	[TestClass]
	public class RouterTests
	{
		[TestMethod]
		public void SimpleOneNodeTest()
		{
			// Create k nodes and register nodes 1 through k-1 onto node 0.
			List<Node> nodes = CreateNodes(Constants.K);
			nodes.Skip(1).ForEach(n => nodes[0].SimpleRegistration(n.OurContact));
			Router router = new Router();
			List<Contact> contacts;

			contacts = router.Lookup(ID.ZeroID(), nodes[0], Dht.NodeLookup).contacts;
			Assert.IsTrue(contacts.Count == 3, "Expected alpha items");
			// Don't forget, we exclude ourselves!
			Assert.IsTrue(contacts[0] == nodes[1].OurContact, "Expected contact 0 to be returned.");

			contacts = router.Lookup(ID.MaxID(), nodes[0], Dht.NodeLookup).contacts;
			Assert.IsTrue(contacts.Count == 3, "Expected alpha items");
			Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected contact 0 to be returned.");
		}

        [TestMethod]
        public void LinearChainForwardCrawlTest()
        {
            Router router = new Router();
            List<Contact> contacts;

            List<Node> nodes = CreateNodes(Constants.K);
            CreateLinearChain(nodes);

            // forward crawl...
            contacts = router.Lookup(ID.MaxID(), nodes[0], Dht.NodeLookup).contacts;
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
            Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected contact 0 to be returned.");
        }

        [TestMethod]
		public void LinearChainBackwardCrawlTest()
		{
			Router router = new Router();
			List<Contact> contacts;

			List<Node> nodes = CreateNodes(Constants.K);
            CreateLinearChain(nodes);

			// backward crawl...
			contacts = router.Lookup(ID.ZeroID(), nodes[Constants.K - 1], Dht.NodeLookup).contacts;
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
			Assert.IsTrue(contacts[0] == nodes[0].OurContact, "Expected contact 0 to be returned.");
		}

        [TestMethod]
        public void CircularChainForwardCrawlTest()
        {
            Router router = new Router();
            List<Contact> contacts;

            List<Node> nodes = CreateNodes(Constants.K);
            CreateCircularChain(nodes);

            // forward crawl...
            contacts = router.Lookup(ID.MaxID(), nodes[0], Dht.NodeLookup).contacts;
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
            Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected node 19 to be returned.");
        }

        [TestMethod]
        public void CircularChainBackwardCrawlTest()
        {
            Router router = new Router();
            List<Contact> contacts;

            List<Node> nodes = CreateNodes(Constants.K);
            CreateCircularChain(nodes);

            // backward crawl...
            contacts = router.Lookup(ID.ZeroID(), nodes[Constants.K - 1], Dht.NodeLookup).contacts;
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
            Assert.IsTrue(contacts[0] == nodes[0].OurContact, "Expected node 0 to be returned.");
        }

        [TestMethod]
        public void NothingToFindTest()
        {
            Router router = new Router();
            Node node = CreateNode(ID.OneID());
            List<Contact> contacts = router.Lookup(ID.ZeroID(), node, Dht.NodeLookup).contacts;

            Assert.IsTrue(contacts.Count == 0, "Expected 0 contacts.");
        }

		[TestMethod]
		public void SimpleDiscoveryTest()
		{
			Node node1 = CreateNode(ID.OneID());
			Node node2 = CreateNode(ID.OneID() << 1);
			node1.SimpleRegistration(node2.OurContact);

			// Initial state:
			Assert.IsTrue(node1.BucketList.GetBucketContactCounts().Count == 1, "Expected only one contact.");
			Assert.IsTrue(node2.BucketList.GetBucketContactCounts().Count == 0, "Expected no contacts.");

			new Router().Lookup(ID.ZeroID(), node1, Dht.NodeLookup);

			// Final state:
			Assert.IsTrue(node1.BucketList.GetBucketContactCounts().Count == 1, "Expected only one contact.");
			Assert.IsTrue(node2.BucketList.GetBucketContactCounts().Count == 1, "Expected one contact.");
			Assert.IsTrue(node2.BucketList.GetBucketContactCounts().First().idx == 0, "Expected contact in bucket 0");
		}

		[TestMethod]
		public void LinearChainDiscoveryTest()
		{
			Router router = new Router();
			List<Node> nodes = CreateNodes(Constants.K);
			CreateLinearChain(nodes);

			nodes.ForEach(n => router.Lookup(ID.MaxID(), n, Dht.NodeLookup));

            // All nodes should have 19 contacts:
            int expectedContacts = Constants.ID_LENGTH_BYTES - 1;

            // Final state:
            nodes.ForEachWithIndex((n, idx) =>
			{
				Assert.IsTrue(n.BucketList.GetBucketContactCounts().Count == expectedContacts, "Expected " + expectedContacts + " contact(s).");
			});
		}

		public static Node CreateNode(ID id)
		{
			var address = new InMemoryNodeAddress();
			Node node = new Node(address, id);
			address.RecipientNode = node;

			return node;
		}

		/// <summary>
		/// Create nodes with known ID's from 1 to 2^n
		/// </summary>
		public static List<Node> CreateNodes(int n)
		{
			List<Node> nodes = new List<Node>();
			ID id = ID.OneID();
			Node node;

			n.ForEach(() =>
			{
				var address = new InMemoryNodeAddress();
				nodes.Add(node = new Node(address, id));
				address.RecipientNode = node;
				id <<= 1;
			});

			return nodes;
		}

		public static void CreateLinearChain(List<Node> nodes)
        {
			int count = nodes.Count;

            // edge cases:
            nodes[0].SimpleRegistration(nodes[1].OurContact);
            nodes[count - 1].SimpleRegistration(nodes[count - 2].OurContact);

            // The rest:
            for (int i = 1; i < count - 1; i++)
            {
                nodes[i].SimpleRegistration(nodes[i - 1].OurContact);
                nodes[i].SimpleRegistration(nodes[i + 1].OurContact);
            }
        }

		public static void CreateCircularChain(List<Node> nodes)
        {
			int count = nodes.Count;

			nodes.ForEachWithIndex((n, i) =>
			{
				nodes[i].SimpleRegistration(nodes[(i - 1).Mod(count)].OurContact);
				nodes[i].SimpleRegistration(nodes[(i + 1).Mod(count)].OurContact);
			});
        }

		public static void CreateStorage(List<Node> nodes)
		{
			nodes.ForEach(n => n.Storage = new InMemoryStorage());
		}
    }
}
