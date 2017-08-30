using System.Collections.Generic;

namespace Clifton.Kademlia.Implementations
{
	/// <summary>
	/// Implements in-memory storage for key-value pairs.
	/// </summary>
	public class InMemoryStorage : IStorage
	{
		protected Dictionary<string, string> storage;

		public InMemoryStorage()
		{
			storage = new Dictionary<string, string>();
		}

		public int Entries()
		{
			return storage.Count;
		}

		public bool HasValue(string key)
		{
			return storage.ContainsKey(key);
		}

		public bool TryGetValue(string key, out string val)
		{
			return storage.TryGetValue(key, out val);
		}

		public string Get(string key)
		{
			return storage[key];
		}

		public void Set(string key, string val)
		{
			storage[key] = val;
		}
	}
}
