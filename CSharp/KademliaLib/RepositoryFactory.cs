using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Persistence;

namespace Kademlia
{
	public static class RepositoryFactory
	{
		public static IKademliaRepository CreateRepository()
		{
			return new KademliaRepository();
		}
	}
}
