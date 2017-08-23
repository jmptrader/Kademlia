using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Kademlia;

namespace Test
{
	class Program
	{
		static void Main(string[] args)
		{
			KademliaRepository repo1 = new KademliaRepository();
			KademliaRepository repo2 = new KademliaRepository();
			KademliaEndpoint ep1 = new KademliaEndpoint();
			KademliaEndpoint ep2 = new KademliaEndpoint();

			KademliaNode node1 = new KademliaNode(repo1, ep1);
			KademliaNode node2 = new KademliaNode(repo2, ep2);

			Thread.Sleep(1000);

			// Not sure what this does or why we need to call it.
			node1.JoinNetwork();
			node2.JoinNetwork();

			// Bootstrap node 2 by having it ping a known server, node 1, at ep1
			bool pinged = node2.Bootstrap(ep1);

			Console.ReadLine();
			node1.Put("Hello World");

			//IList<string> foundVals = node1.Get(babiesID);
			//foreach (string s in foundVals)
			//{
			//	Console.WriteLine("1 Found: " + s);
			//}
		}
	}
}
