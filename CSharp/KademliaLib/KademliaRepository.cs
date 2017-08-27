using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Persistence;
using Persistence.Tag;

namespace Kademlia
{
	// TODO: Implements an in-memory repo.  Need to abstract out implementation details.
	public class KademliaRepository : IKademliaRepository
	{
		// private Repository _repository;
		private TimeSpan _elementValidity = new TimeSpan(24, 0, 0);

		// ============================================================================================

		// TODO: abstract out, as this is the in-memory implementation.

		private Dictionary<string, KademliaResource> resources = new Dictionary<string, KademliaResource>();

		private List<KademliaResource> GetAllResources()
		{
			return new List<KademliaResource>(resources.Values);
		}

		private bool TryGetByKey(string hash, out KademliaResource res)
		{
			return resources.TryGetValue(hash, out res);
		}

		// TODO: Is this correct?
		private void Save(KademliaResource rs)
		{
			resources[rs.Tag.Hash] = rs;
		}

		private KademliaResource GetResource(string hash)
		{
			KademliaResource rs;

			resources.TryGetValue(hash, out rs);

			return rs;
		}

		// ============================================================================================

		// TODO: not sure what this does or if it's properly represented.
		private Dictionary<string, Dictionary<string, List<DhtElement>>> elements = new Dictionary<string, Dictionary<string, List<DhtElement>>>();

		private void AddElement(string id, string key, DhtElement el)
		{
			Dictionary<string, List<DhtElement>> idVal;

			if (!elements.TryGetValue(id, out idVal))
			{
				idVal = new Dictionary<string, List<DhtElement>>();
				idVal[key] = new List<DhtElement>();
				elements[id] = idVal;
			}

			if (!idVal.ContainsKey(key))
			{
				idVal[key] = new List<DhtElement>();
			}

			idVal[key].Add(el);
		}

		private void SetElement(string id, string key, DhtElement el, DateTime pubtime)
		{
			Dictionary<string, List<DhtElement>> idVal;

			if (!elements.TryGetValue(id, out idVal))
			{
				idVal = new Dictionary<string, List<DhtElement>>();
				idVal[key] = new List<DhtElement>();
				elements[id] = idVal;
			}

			int idx = idVal[key].FindIndex(q => q == el);

			if (idx == -1)
			{
				// TODO: Does this scenario happen?
				el.Publication = pubtime;			// TODO: Is this correct?
				idVal[key].Add(el);
			}
			else
			{
				idVal[key][idx].Publication = pubtime;
			}
		}

		private void RemoveElement(string id, string key, List<DhtElement> elementsToRemove)
		{
			Dictionary<string, List<DhtElement>> idVal;

			if (elements.TryGetValue(id, out idVal))
			{
				idVal[key].RemoveAll(q => elementsToRemove.Contains(q));
			}
		}

		// ============================================================================================

		/// <summary>
		/// Stores a tag as resource into the kademlia repository. If the resource already exists the method tries to add the 
		/// given Url (with the desired publication time) to the list of suppliers for that resource; if the url is already known
		/// the method does nothing. If the resource is new first the method adds it to the repository and then generates a set of
		/// keywords related to the new resource. For each generated keyword, if this is already in the repository the tag
		/// identifier (that is the resource identifier too) will be added to the related tags list, if the keyword doesn't exist
		/// it will be created and its related tags list will contains only the new resource.
		/// </summary>
		/// <param name="tag">Tag to store in the kademlia resource</param>
		/// <param name="peer">Url of the supplier</param>
		/// <param name="pubtime">Publication TIme</param>
		/// <returns>False if something went wrong, true otherwise</returns>
		public bool StoreResource(CompleteTag tag, Uri peer, DateTime pubtime)
		{
			KademliaResource rs = new KademliaResource();
			Console.WriteLine("Storing resource from peer " + peer);
			DhtElement dhtElem = new DhtElement(peer, pubtime, this._elementValidity);

			bool found = TryGetByKey(tag.Hash, out rs);

			if (found)
			{
				if (!rs.Urls.Contains(dhtElem))
				{
					AddElement(rs.Id, "Urls", dhtElem);
				}
				else
				{
					// log.Debug("Urls " + peer.ToString() + " already known");
				}
			}
			else
			{
				rs = new KademliaResource(tag, dhtElem);
				Save(rs);

				// This seems to have to do with key searches, which we ignore.
				/*
				List<string> pks = new List<string>(generatePrimaryKey(tag));
				List<KademliaKeyword> keys = new List<KademliaKeyword>();
				if (_repository.GetByKeySet(pks.ToArray(), keys) > 0)
				{
					foreach (KademliaKeyword k in keys)
					{
						if (!k.Tags.Contains(rs.Id))
						{
							_repository.ArrayAddElement(k.Id, "Tags", rs.Id);
						}
						pks.Remove(k.Id);
					}
					foreach (String pk in pks)
					{
						KademliaKeyword localKey = new KademliaKeyword(pk, rs.Id);
						_repository.Save(localKey);
					}
				}
				*/
			}

			return true;
		}

		// WE DO NOT IMPLEMENT SEARCH BY KEYWORD
		/*
		/// <summary>
		/// Performs search query over the repository. This split query in pieces and search for keywords that contains those pieces.
		/// Then for each keyword the method loads all the related resource. 
		/// </summary>
		/// <param name="query">Query string</param>
		/// <returns>An array containing the resource found</returns>
		public KademliaResource[] SearchFor(string query)
		{
			List<KademliaResource> resources = new List<KademliaResource>();

			List<KademliaKeyword> keys = new List<KademliaKeyword>();
			string[] queryParts = query.Split(' ');
			_repository.GetAllByCondition(kw =>
			{
				string kid = kw.Id.Substring(17);
				foreach (string p in queryParts)
				{
					if (kid.Contains(p.ToLower()))
					{
						return true;
					}
				}
				return false;
			}, keys);
			List<string> tids = new List<string>();
			foreach (KademliaKeyword kw in keys)
			{
				tids.AddRange(kw.Tags);
			}
			_repository.GetByKeySet(tids.ToArray(), resources);
			return resources.ToArray();
		}
		*/
		// INSTEAD, WE GET THE RESOURCE ASSOCIATED WITH THE KEY.
		// TODO: What happens when we have multiple resources associated with the same key, as the original test implied can happen?
		public KademliaResource[] SearchFor(string key)
		{
			List<KademliaResource> resources = new List<KademliaResource>();
			KademliaResource rs;

			if (TryGetByKey(key, out rs))
			{
				resources.Add(rs);
			}

			return resources.ToArray();
		}

		// TODO: Why are we returning a LinkedList?
		// TODO: This is very much tied to our in-memory implementation.

		/// <summary>
		/// Returns a list of all resources of the repository.
		/// </summary>
		/// <returns>Linked List containing all the resources</returns>
		public LinkedList<KademliaResource> GetAllElements()
		{
			return new LinkedList<KademliaResource>(GetAllResources());
		}

		/// <summary>
		/// Checks if the given resource contains the Url in its supplier list
		/// </summary>
		/// <param name="tagid">Resource identifier</param>
		/// <param name="url">Url of the supplier</param>
		/// <returns>True if the Peer with the given Url is a supplier for the resource.</returns>
		public bool ContainsUrl(string tagid, Uri url)
		{
			KademliaResource rs = Get(tagid);
			DhtElement fakeElem = new DhtElement()
			{
				Url = url
			};
			if (rs != null)
			{
				if (rs.Urls.Contains(fakeElem))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the Resource identified by the given taghash.
		/// </summary>
		/// <param name="tagid">Identifier of the resource to get</param>
		/// <returns>The requested resource if it exists, null if not.</returns>
		public KademliaResource Get(string tagid)
		{
			return GetResource(tagid);
		}

		/// <summary>
		/// Returns the publication time of a Dht Element related to a Resource.
		/// </summary>
		/// <param name="tagid">Resource Identifier</param>
		/// <param name="url">Url of the Dht Element</param>
		/// <returns>The publication time of the Dht Element if it exists, the DateTime min value if the element doesn't exist</returns>
		public DateTime GetPublicationTime(string tagid, Uri url)
		{
			KademliaResource rs = Get(tagid);
			if (rs != null)
			{
				DhtElement elem = rs.Urls.ToList().Find(de =>
				{
					return de.Url.Equals(url);
				}
				);
				if (elem != null)
				{
					return elem.Publication;
				}
			}
			return DateTime.MinValue;
		}

		// TODO: This assumes a 1:1 mapping of rs.Urls with this concept of "elements"
		// Also, the original implementation is closely tied to Raven.  What we want to do here is directly set the elem.Publication.

		/// <summary>
		/// Sets the publication time of a Dht Element in order to avoid expiration
		/// </summary>
		/// <param name="tagid">Resource Identifier</param>
		/// <param name="url">URL of the Dht Element to refresh</param>
		/// <param name="pubtime">Publication time to set</param>
		/// <returns>True if the Dht Element exists and has been refreshed, false otherwise</returns>
		public bool RefreshResource(string tagid, Uri url, DateTime pubtime)
		{
			KademliaResource rs;
			bool found = TryGetByKey(tagid, out rs);
			
			if (found)
			{
				var dhtElement = rs.Urls.ToList().SingleOrDefault(elem => elem.Url.Equals(url));

				if (dhtElement != null)
				{
					SetElement(tagid, "Urls", dhtElement, pubtime);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Method that removes the expired elements from the DHT. This method search for expired element and add it to a structure,
		/// then delete all the expired elements and if a resource remanins without any supplier will be deleted too.
		/// </summary>
		public void Expire()
		{
			List<KademliaResource> lr = GetAllResources();
			LinkedList<ExpireIteratorDesc> cleanList = new LinkedList<ExpireIteratorDesc>();

			Parallel.ForEach(lr,
				() => new ExpireIteratorDesc(),
				(key, loop, iter_index, iter_desc) =>
				{
					if (key == null) return iter_desc;
					if (iter_desc.TagId == null)
					{
						iter_desc.TagId = key.Id;
					}
					List<DhtElement> dhtElementList = key.Urls.ToList();
					for (int k = 0; k < dhtElementList.Count; k++)
					{
						DhtElement delem = dhtElementList[k];
						if (DateTime.Compare(delem.Publication.Add(delem.Validity), DateTime.Now) <= 0)
						{
							iter_desc.Expired.Add(delem);
						}
					}
					if (iter_desc.Expired.Count == key.Urls.Count)
					{
						iter_desc.ToBeDeleted = true;
					}
					else
					{
						iter_desc.ToBeDeleted = false;
					}
					return iter_desc;
				},
				(finalResult) => cleanList.AddLast(finalResult)
			);
			Parallel.ForEach(cleanList,
				(iter_desc) =>
				{
					if (iter_desc == null) return;

					if (iter_desc.ToBeDeleted)
					{
						// DeleteTag(iter_desc.TagId);
					}
					else
					{
						RemoveElement(iter_desc.TagId, "Urls", iter_desc.Expired);
					}
				}
			);
		}

		// REMOVES A TAG -- THIS WAS RELATED TO THE ORIGINAL CODE AND THE ABILITY TO HAVE KEYWORDS/TAGS ASSOCIATED WITH DATA.
		/*
		/// <summary>
		/// Deletes a tag with a given identifier. This method finds all keywords containing a reference to the tag to be deleted and
		/// removes the tag identifier from the keyword's list; then it search for keywords that has an empty list and deletes them.
		/// At the end the method removes the resource from the repository
		/// </summary>
		/// <param name="tid">Identifier of the tag to be deleted</param>
		/// <returns>False if something went wrong, true otherwise</returns>
		public bool DeleteTag(string tid)
		{
			
			List<KademliaKeyword> results = new List<KademliaKeyword>();
			this._repository.QueryOverIndex("KademliaKeywords/KeysByTag", "Tid:" + tid, results);
			foreach (var t in results)
			{
				//t.Tags.FindIndex(x => x.Equals(tid))
				this._repository.ArrayRemoveElement(t.Id, "Tags", tid);
			}
			this._repository.Delete<KademliaResource>(tid);
			results.Clear();
			if (this._repository.QueryOverIndex("KademliaKeywords/EmptyKeys", "", results) != RepositoryResponse.RepositoryLoad)
			{
				return false;
			}
			string[] ids = new string[results.Count];
			int index = 0;
			foreach (var t in results)
			{
				ids[index++] = t.Id;
			}
			this._repository.BulkDelete<KademliaKeyword>(ids);
			
			return true;
		}
		*/

		/// <summary>
		/// Private class to store Expire method iteration information.
		/// </summary>
		private class ExpireIteratorDesc
		{
			/// <summary>
			/// Tag Identifier for the iteration
			/// </summary>
			public string TagId
			{
				get;
				set;
			}
			/// <summary>
			/// Flag that indicates that the tag has to be completely deleted
			/// </summary>
			public bool ToBeDeleted
			{
				get;
				set;
			}
			/// <summary>
			/// List of expired element
			/// </summary>
			public List<DhtElement> Expired
			{
				get;
				set;
			}
			/// <summary>
			/// Default constructor.
			/// </summary>
			public ExpireIteratorDesc()
			{
				TagId = null;
				ToBeDeleted = false;
				Expired = new List<DhtElement>();
			}
		}

		/// <summary>
		/// Method used to dispose the repository
		/// </summary>
		//public void Dispose()
		//{
		//	 _repository.Dispose();
		//}
	}
}
