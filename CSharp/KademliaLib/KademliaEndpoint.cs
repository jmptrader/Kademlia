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
		}

		public void HandleFindNodeResponse(FindNodeResponse response)
		{
		}

		public void HandleFindValue(FindValue question)
		{
		}

		public void HandleFindValueContactResponse(FindValueContactResponse response)
		{
			throw new NotImplementedException();
		}

		public void HandleFindValueDataResponse(FindValueDataResponse response)
		{
			throw new NotImplementedException();
		}

		public void HandlePong(Pong pong)
		{
			throw new NotImplementedException();
		}

		public void HandleStoreData(StoreData r)
		{
			throw new NotImplementedException();
		}

		public void HandleStoreQuery(StoreQuery storeIt)
		{
			throw new NotImplementedException();
		}

		public void HandleStoreResponse(StoreResponse response)
		{
			throw new NotImplementedException();
		}
	}
}
