using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Persistence.Tag;

namespace Persistence
{
	public interface IKademliaRepository
	{
		bool StoreResource(CompleteTag tag, Uri peer, DateTime pubtime);
		KademliaResource[] SearchFor(string query);
		void Expire();
		LinkedList<KademliaResource> GetAllElements();
		bool ContainsUrl(string tagid, Uri url);
		DateTime GetPublicationTime(string tagid, Uri url);
		bool RefreshResource(string tagid, Uri url, DateTime pubtime);
	}
}
