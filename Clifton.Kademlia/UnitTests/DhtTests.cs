using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests
{
	[TestClass]
	public class DhtTests
	{
		[TestMethod]
		public void StoreTest()
		{
			List<Node> nodes = RouterTests.CreateNodes(Constants.K * 2);
			RouterTests.CreateLinearChain(nodes);
			RouterTests.CreateStorage(nodes);
			Dht dht = new Dht(nodes[0]);
			dht.Store(ID.FromString("A"), "1");	// steak sauce
			int stores = nodes.Sum(n => n.Storage.Entries());
			Assert.IsTrue(stores == 21, "Expected 21 storage locations (ourself plus k more)");
		}

		[TestMethod]
		public void FindValueTest()
		{
			List<Node> nodes = RouterTests.CreateNodes(Constants.K * 2);
			RouterTests.CreateLinearChain(nodes);
			RouterTests.CreateStorage(nodes);
			Dht dht = new Dht(nodes[0]);
			dht.Store(ID.FromString("A"), "1");    // steak sauce

			Node emptyNode = nodes.First(n => n.Storage.Entries() == 0);
			// Create a DHT associated with this node.
			Dht dht2 = new Dht(emptyNode);
			var result = dht2.FindValue(ID.FromString("A"));

			Assert.IsTrue(result.found, "Expected value to be found");
			Assert.IsTrue(result.val == "1", "Expected A1 steak sauce.");
		}
	}
}