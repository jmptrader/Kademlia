using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


			node1.JoinNetwork();
			node2.JoinNetwork();

			ID babiesID = ID.RandomID();
			node1.Put("Hello World");

			//IList<string> foundVals = node1.Get(babiesID);
			//foreach (string s in foundVals)
			//{
			//	Console.WriteLine("1 Found: " + s);
			//}
		}
	}
}
