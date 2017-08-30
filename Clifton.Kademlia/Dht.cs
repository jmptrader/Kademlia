using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Kademlia
{
	public class Dht
	{
		protected Node node;

		public Dht(Node node)
		{
			this.node = node;
		}

		public void Store(string key, string val)
		{
		}
	}
}
