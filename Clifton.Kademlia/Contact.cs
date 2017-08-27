using System;

namespace Clifton.Kademlia
{
	public class Contact
	{
		public DateTime LastSeen { get; set; }
		public IAddress Address { get; set; }
		public ID NodeID { get; set; }

		public void Touch()
		{
			LastSeen = DateTime.Now;
		}
	}
}
