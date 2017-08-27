using System.Collections.Generic;
using System.Net;

namespace Clifton.Kademlia.Implementations
{
	public class UdpAddress : IAddress
	{
		public IPAddress Address { get; set; }
		public int Port { get; set; }

		public Contact Ping(Contact sender, IAddress recipient, ID randomID)
		{
			return null;
		}

		public void Store(Contact sender, IAddress recipient, ID randomID, string key, string val)
		{
		}

		public List<Contact> FindNode(Contact sender, IAddress recipient, ID randomID, ID toFind)
		{
			return null;
		}

		public (List<Contact> nodes, string val) FindValue(Contact sender, IAddress recipient, ID randomID, string key)
		{
			return (null, null);
		}
	}
}
