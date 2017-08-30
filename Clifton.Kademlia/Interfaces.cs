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
		int Entries();
		bool HasValue(string key);
		bool TryGetValue(string key, out string val);
		string Get(string key);
		void Set(string key, string val);
	}

	public interface IAddress
	{
		Contact Ping(Contact sender, IAddress recipient, ID randomID);
		void Store(Contact sender, IAddress recipient, ID randomID, ID keyID, string val);
		(List<Contact> nodes, string val) FindNode(Contact sender, IAddress recipient, ID randomID, ID toFind);
		(List<Contact> nodes, string val) FindValue(Contact sender, IAddress recipient, ID randomID, ID keyID);
	}
}
