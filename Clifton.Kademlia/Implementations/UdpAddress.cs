using System.Net;

namespace Clifton.Kademlia.Implementations
{
	public class UdpAddress : IAddress
	{
		public IPAddress Address { get; set; }
		public int Port { get; set; }
	}
}
