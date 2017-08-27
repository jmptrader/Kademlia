using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Kademlia.Messages;

namespace Kademlia
{
	public interface IKademliaEndpoint
	{
		Uri Uri { get; set; }
		// IKademliaNode CreateNode();
		KademliaNode Node { get; set; }
		void HandleStoreQuery(StoreQuery storeIt);
		void HandleFindNode(FindNode question);
		void HandleFindValue(FindValue question);
		void HandlePing(Ping ping);
		void HandlePong(Pong pong);
		void HandleFindNodeResponse(FindNodeResponse response);
		void HandleFindValueDataResponse(FindValueDataResponse response);
		void HandleFindValueContactResponse(FindValueContactResponse response);
		void HandleStoreResponse(StoreResponse response);
		void HandleStoreData(StoreData r);
	}
}
