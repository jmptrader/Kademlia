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
		public void ChainTest()
		{
			Router router = new Router();
			List<Contact> contacts;

			// Create k nodes and register nodes 1 through k-1 onto node 0.
			List<Node> nodes = CreateNodes(Constants.K);
			// edge cases:
			nodes[0].SimpleRegistration(nodes[1].OurContact);
			nodes[Constants.K - 1].SimpleRegistration(nodes[Constants.K - 2].OurContact);
			
			// The rest:
			for (int i = 1; i < Constants.K - 1; i++)
			{
				nodes[i].SimpleRegistration(nodes[i - 1].OurContact);
				nodes[i].SimpleRegistration(nodes[i + 1].OurContact);
			}

			// backward crawl...
			contacts = router.NodeLookup(ID.ZeroID(), nodes[Constants.K - 1]);
			Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
			Assert.IsTrue(contacts[0] == nodes[0].OurContact, "Expected contact 0 to be returned.");

			// forward crawl...
			contacts = router.NodeLookup(ID.MaxID(), nodes[0]);
			Assert.IsTrue(contacts.Count == Constants.K - 1, "Expected alpha items");
			Assert.IsTrue(contacts[0] == nodes[Constants.K - 1].OurContact, "Expected contact 0 to be returned.");
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
	}
}
