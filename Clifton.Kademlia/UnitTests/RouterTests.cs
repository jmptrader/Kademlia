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

			contacts = router.NodeLookup(ID.ZeroID(), nodes[0]);
			Assert.IsTrue(contacts.Count == 3, "Expected alpha items");
			// Don't forget, we exclude ourselves!
			Assert.IsTrue(contacts[0] == nodes[1].OurContact, "Expected contact 0 to be returned.");

			contacts = router.NodeLookup(ID.MaxID(), nodes[0]);
			Assert.IsTrue(contacts.Count == 3, "Expected alpha items");
			Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected contact 0 to be returned.");
		}

        [TestMethod]
        public void LinearChainForwardCrawlTest()
        {
            Router router = new Router();
            List<Contact> contacts;

            // Create k nodes and register nodes 1 through k-1 onto node 0.
            List<Node> nodes = CreateNodes(Constants.K);
            CreateLinearChain(nodes);

            // forward crawl...
            contacts = router.NodeLookup(ID.MaxID(), nodes[0]);
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
            Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected contact 0 to be returned.");
        }

        [TestMethod]
		public void LinearChainBackwardCrawlTest()
		{
			Router router = new Router();
			List<Contact> contacts;

			// Create k nodes and register nodes 1 through k-1 onto node 0.
			List<Node> nodes = CreateNodes(Constants.K);
            CreateLinearChain(nodes);

			// backward crawl...
			contacts = router.NodeLookup(ID.ZeroID(), nodes[Constants.K - 1]);
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
			Assert.IsTrue(contacts[0] == nodes[0].OurContact, "Expected contact 0 to be returned.");
		}

        [TestMethod]
        public void CircularChainForwardCrawlTest()
        {
            Router router = new Router();
            List<Contact> contacts;

            // Create k nodes and register nodes 1 through k-1 onto node 0.
            List<Node> nodes = CreateNodes(Constants.K);
            CreateCircularChain(nodes);

            // forward crawl...
            contacts = router.NodeLookup(ID.MaxID(), nodes[0]);
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
            Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected node 19 to be returned.");
        }

        [TestMethod]
        public void CircularChainBackwardCrawlTest()
        {
            Router router = new Router();
            List<Contact> contacts;

            // Create k nodes and register nodes 1 through k-1 onto node 0.
            List<Node> nodes = CreateNodes(Constants.K);
            CreateCircularChain(nodes);

            // backward crawl...
            contacts = router.NodeLookup(ID.ZeroID(), nodes[Constants.K - 1]);
            // Note that after the node lookup, the kbuckets have been updated with the requestors.

            Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
            Assert.IsTrue(contacts[0] == nodes[0].OurContact, "Expected node 0 to be returned.");
        }

        [TestMethod]
        public void NothingToFindTest()
        {
            Router router = new Router();
            Node node = CreateNodes(1).First();
            List<Contact> contacts = router.NodeLookup(ID.ZeroID(), node);

            Assert.IsTrue(contacts.Count == 0, "Expected 0 contacts.");
        }

        /// <summary>
        /// Create nodes with known ID's from 1 to 2^n
        /// </summary>
        private List<Node> CreateNodes(int n)
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

        private void CreateLinearChain(List<Node> nodes)
        {
            // edge cases:
            nodes[0].SimpleRegistration(nodes[1].OurContact);
            nodes[Constants.K - 1].SimpleRegistration(nodes[Constants.K - 2].OurContact);

            // The rest:
            for (int i = 1; i < Constants.K - 1; i++)
            {
                nodes[i].SimpleRegistration(nodes[i - 1].OurContact);
                nodes[i].SimpleRegistration(nodes[i + 1].OurContact);
            }
        }

        private void CreateCircularChain(List<Node> nodes)
        {
            for (int i = 0; i < Constants.K; i++)
            {
                nodes[i].SimpleRegistration(nodes[(i - 1).Mod(Constants.K)].OurContact);
                nodes[i].SimpleRegistration(nodes[(i + 1).Mod(Constants.K)].OurContact);
            }
        }

/*
        // This got way to complicated
        // TODO: Analyze this carefully, as it currently fails and has all sorts of goofy logic.
        [TestMethod]
        public void LinearChainContactCounts()
        {
            Router router = new Router();

            // Create k nodes and register nodes 1 through k-1 onto node 0.
            List<Node> nodes = CreateNodes(Constants.K);
            CreateLinearChain(nodes);

            // Preconditions (note bucket 0 is never populated because our node ID starts with 1, not 0)

            // Edge cases:
            var counts = nodes[0].BucketList.GetBucketContactCounts();
            Assert.IsTrue(counts.Count == 1);
            Assert.IsTrue(counts.First().idx == 1);
            Assert.IsTrue(counts.First().count == 1);

            counts = nodes[Constants.K - 1].BucketList.GetBucketContactCounts();
            Assert.IsTrue(counts.Count == 1);
            Assert.IsTrue(counts.First().idx == Constants.K - 1);
            Assert.IsTrue(counts.First().count == 1);

            // nodes 1-18:
            nodes.Skip(1).Take(Constants.K - 2).ForEachWithIndex((n, idx) =>
            {
                counts = n.BucketList.GetBucketContactCounts();
                Assert.IsTrue(counts.Count == 2);
                Assert.IsTrue(counts.First().idx == idx + 1);
                Assert.IsTrue(counts.First().count == 1);
                Assert.IsTrue(counts.Second().idx == idx + 2);
                Assert.IsTrue(counts.Second().count == 1);
            });

            // have each node attempt to find the closest node.  This forces the k-bbuckets to fill
            // up as we start the search from each node that knows only 1 (edge cases) or 2 (middle) other nodes.
            // We skip ourself as the last contact because the last contact contains the ID -- it's nearest contact would be node[18]
            nodes.Take(Constants.K - 1).ForEach(n =>
            {
                List<Contact> contacts = router.NodeLookup(ID.MaxID(), n);
                // In all cases, we should get the last node as the closest.
                Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected contact 0 to be returned.");
            });

            // But we still process it:
            router.NodeLookup(ID.MaxID(), nodes[Constants.K - 1]);

            // Each node should now have 19 k-buckets each with 1 contact:
            nodes.ForEachWithIndex((n, idx) =>
            {
                counts = n.BucketList.GetBucketContactCounts();
                Assert.IsTrue(counts.Count == (idx < 2 ? Constants.K - 1 : Constants.K - idx));
                counts.ForEachWithIndex((c, q) =>
                {
                    Assert.IsTrue(c.idx == q + (idx < 2 ? 1 : idx));
                    Assert.IsTrue(c.count == (idx < 2 ? 1 : 2));
                });
            });
        }
*/
    }
}
