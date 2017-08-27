using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Kademlia
{
	public interface IRouter { }
	public interface ICommunication { }

	public interface IStorage
	{
		string Get(string key);
		void Set(string key, string val);
	}

	public interface IAddress { }
}
