using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Kademlia
{
	public interface IRouter { }

	public interface IStorage
	{
		string Get(string key);
		void Set(string key, string val);
	}

	public interface IAddress
	{
		Contact Ping(Contact sender, IAddress recipient, ID randomID);
		void Store(Contact sender, IAddress recipient, ID randomID, string key, string val);
		List<Contact> FindNode(Contact sender, IAddress recipient, ID randomID, ID toFind);
		(List<Contact> nodes, string val) FindValue(Contact sender, IAddress recipient, ID randomID, string key);
	}
}
