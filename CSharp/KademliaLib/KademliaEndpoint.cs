using System;
using Kademlia.Messages;

using Persistence;

namespace Kademlia
{
	public class KademliaEndpoint : IKademliaEndpoint
	{
		public Uri Uri { get; set; }
		public IKademliaRepository Repository { get; set; }
		public KademliaNode Node { get; set; }

		public IKademliaNode CreateNode()
		{
			Repository = RepositoryFactory.CreateRepository();
			Node = new KademliaNode(Repository);

			return Node;
		}

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

		public void HandlePong(Pong pong)
		{
			Node.HandlePong(pong);
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
