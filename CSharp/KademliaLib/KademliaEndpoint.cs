using System;
using System.Collections.Generic;
using Kademlia.Messages;

using Persistence;

namespace Kademlia
{
	public class KademliaEndpoint : IKademliaEndpoint
	{
		// For testing, we keep a dictionary of all actual endpoints created in-memory.
		public static Dictionary<string, IKademliaEndpoint> actualEndpoints = new Dictionary<string, IKademliaEndpoint>();

		public Uri Uri { get; set; }
		public IKademliaRepository Repository { get; set; }
		public KademliaNode Node { get; set; }

		public KademliaEndpoint(string endpointName)
		{
			// TODO: Here we are creating a unique URI for testing purposes.
			string fullPath = ("http://" + endpointName + "/").ToLower();  // Uri ToString is always all lowercase.
			Uri = new Uri(fullPath);
			actualEndpoints[fullPath] = this;
		}

		public KademliaEndpoint(Uri uri)
		{
			// Here we create a psuedo endpoint to send the something to, like a Pong
			Uri = uri;
		}

		//public IKademliaNode CreateNode()
		//{
		//	Repository = RepositoryFactory.CreateRepository();
		//	Node = new KademliaNode(Repository, this);

		//	return Node;
		//}

		public void HandleFindNode(FindNode question)
		{
			Node.HandleFindNode(question);
		}

		public void HandleFindNodeResponse(FindNodeResponse response)
		{
			Node.HandleFindNodeResponse(response);
		}

		public void HandleFindValue(FindValue question)
		{
			Node.HandleFindValue(question);
		}

		public void HandleFindValueContactResponse(FindValueContactResponse response)
		{
			Node.HandleFindValueContactResponse(response);
		}

		public void HandleFindValueDataResponse(FindValueDataResponse response)
		{
			Node.HandleFindValueDataResponse(response);
		}

		public void HandlePing(Ping ping)
		{
			// Send ping message to Uri.
			// Here we simulate handling the ping with the Node associated with this Uri.
			// The endpoint pinging us is in ping.NodeEndpoint
			Node.HandlePing(ping);
		}

		public void HandlePong(Pong pong)
		{
			// Send pong message to Uri.
			// Here we simulate handling the ping with the Node associated with this Uri.
			// The endpoint ponging us is in pong.NodeEndpoint.
			// Since we don't know the actual endpoint node, we have to look it up in our simulator using the this pseudo-construct.
			IKademliaEndpoint endpoint = actualEndpoints[Uri.ToString()];
			endpoint.Node.HandlePong(pong);
		}

		public void HandleStoreData(StoreData r)
		{
			Node.HandleStoreData(r);
		}

		public void HandleStoreQuery(StoreQuery storeIt)
		{
			Node.HandleStoreQuery(storeIt);
		}

		public void HandleStoreResponse(StoreResponse response)
		{
			Node.HandleStoreResponse(response);
		}
	}
}
