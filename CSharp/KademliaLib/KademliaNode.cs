/*****************************************************************************************
 *  p2p-player
 *  An audio player developed in C# based on a shared base to obtain the music from.
 * 
 *  Copyright (C) 2010-2011 Dario Mazza, Sebastiano Merlino
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *  
 *  Dario Mazza (dariomzz@gmail.com)
 *  Sebastiano Merlino (etr@pensieroartificiale.com)
 *  Full Source and Documentation available on Google Code Project "p2p-player", 
 *  see <http://code.google.com/p/p2p-player/>
 *
 ******************************************************************************************/

﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using Kademlia.Messages;
using UdpTransportBinding;
using Persistence.Tag;
using Persistence;
using System.Configuration;
using log4net;

namespace Kademlia
{
	/// <summary>
    /// Functions as a peer in the overlay network. Because it is necessary for the system to have a memory
    /// of the messages arrived and sent, the service is implemented in Singleton. The use of singleton has a bigger
    /// bad side-effect that exclude the possibility to have more than one method of the singleton class
    /// executing on WCF at the same time. In order to bypass this problem has been activated the multiple
    /// concurrency mode and have been used the system threadpool to execute all interfaces method as delegates.
	/// </summary>   
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class KademliaNode : IKademliaNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(KademliaNode));

        #region identity
        private ID nodeID;
        private EndpointAddress nodeEndpoint;
        private EndpointAddress transportEndpoint;
        #endregion

        #region NetworkState
        private BucketList contactCache;
		private Thread bucketMinder; // Handle updates to cache
		private List<Contact> contactQueue; // Add contacts here to be considered for caching
		private const int MAX_QUEUE_LENGTH = 10;
        private const string DEFAULT_TRANSPORT_ENDPOINT = "soap.udp://localhost:9997/transport_protocol/";
        private const string DEFAULT_ENDPOINT = "soap.udp://localhost:10000/kademlia";
        private const string DEFAULT_REPOSITORY = @"..\..\Resource\KademliaDatabase";
        #endregion

        #region caches_and_stores
        // Response cache
		// We want to be able to synchronously wait for responses, so we have other threads put them in this cache.
		// We also need to discard old ones.
		private struct CachedResponse {
			public Response response;
			public DateTime arrived;
		}
		private SortedList<ID, CachedResponse> responseCache;
        private AutoResetEvent responseCacheLocker;

        private KademliaRepository datastore; // Application datastore
		private SortedList<ID, DateTime> acceptedStoreRequests; // Store a list of what put requests we actually accepted while waiting for data.
		// The list of put requests we sent is more complex
		// We need to keep the data and timestamp, but don't want to insert it in our storage.
		// So we keep it in a cache, and discard it if it gets too old.
		private struct OutstandingStoreRequest {
			public ID key;
			public CompleteTag val;
			public DateTime publication;
			public DateTime sent;
		}
		private SortedList<ID, OutstandingStoreRequest> sentStoreRequests;
        #endregion

        #region threads
        // We need a thread to go through and expire all these things
		private Thread authMinder;
        // We need another thread to eliminate old expired things also from stable persistence
        private Thread maintainanceMinder;
        #endregion

        #region timers
        private long max_time = 5000000; // 5 secs. TODO: put it to 500 ms in ticks
        private const int CHECK_INTERVAL = 100;
        private TimeSpan MAX_SYNC_WAIT = new TimeSpan(5000000); // 5 secs. TODO: put it to 500 ms in ticks
		private TimeSpan MAX_CACHE_TIME = new TimeSpan(0, 0, 30);
		
		// How much clock skew do we tolerate?
		private TimeSpan MAX_CLOCK_SKEW = new TimeSpan(1, 0, 0);
        
		// Kademlia config
		private const int PARALELLISM = 3; // Number of requests to run at once for iterative operations.
		private const int NODES_TO_FIND = 20; // = k = bucket size
		private static TimeSpan EXPIRE_TIME = new TimeSpan(24, 0, 0); // Time since original publication to expire a value
		private static TimeSpan REFRESH_TIME = new TimeSpan(1, 0, 0); // Time since last bucket access to refresh a bucket
		private static TimeSpan REPLICATE_TIME = new TimeSpan(1, 0, 0); // Interval at which we should re-insert our whole database
		private DateTime lastReplication;
		private static TimeSpan REPUBLISH_TIME = new TimeSpan(23, 0, 0); // Interval at which we should re-insert our values with new publication times
		
		// How often do we run high-level maintainance (expiration, etc.)
		private static TimeSpan MAINTAINANCE_INTERVAL = new TimeSpan(0, 10, 0);
        #endregion
		
		#region Setup	
		
		/// <summary>
		/// Make a node on a random available port, using an ID specific to this machine. It uses as address the deault endpoint.
		/// </summary>
		public KademliaNode() : this(new EndpointAddress(DEFAULT_ENDPOINT), ID.HostID(), new EndpointAddress(DEFAULT_TRANSPORT_ENDPOINT))
		{
			// Nothing to do!
		}
		
		/// <summary>
		/// Make a node with a specified ID.
		/// </summary>
		/// <param name="id">The ID defined for the node</param>
		public KademliaNode(ID id) : this(new EndpointAddress(DEFAULT_ENDPOINT), id, new EndpointAddress(DEFAULT_TRANSPORT_ENDPOINT))
		{
			// Nothing to do!
		}
		
		/// <summary>
		/// Make a node on a specified address.
		/// </summary>
		/// <param name="addr">The address chosen for the node</param>
		public KademliaNode(EndpointAddress addr) : this(addr, ID.HostID(), new EndpointAddress(DEFAULT_TRANSPORT_ENDPOINT))
		{
			// Nothing to do!
		}

        /// <summary>
        /// Makes a node in the specified address and using the specified transport address
        /// </summary>
        /// <param name="addr">The address of the node</param>
        /// <param name="transportAddr">The address of the transport layer</param>
        public KademliaNode(EndpointAddress addr, EndpointAddress transportAddr)
            : this(addr, ID.HostID(), transportAddr)
        {
            // Nothing to do!
        }
		
		/// <summary>
		/// Make a node on a specific address, with a specified ID and a specific transport Address.
        /// This method initializes chaches, datastore and buckets. At the end it runs maintenance methods.
		/// </summary>
		/// <param name="addr">The address of the node</param>
        /// <param name="id">The ID of the node</param>
        /// <param name="transportAddr">The transport layer address of the node</param>
		public KademliaNode(EndpointAddress addr, ID id, EndpointAddress transportAddr)
		{
			// Set up all our data
            AppSettingsReader asr = new AppSettingsReader();
            nodeEndpoint = addr;
            transportEndpoint = transportAddr;
			nodeID = id;
			contactCache = new BucketList(nodeID);
			contactQueue = new List<Contact>();
            RepositoryConfiguration conf = new RepositoryConfiguration(new { data_dir = (string) asr.GetValue("KademliaRepository", typeof(string)) });
            datastore = new KademliaRepository("Raven", conf);
			acceptedStoreRequests = new SortedList<ID, DateTime>();
			sentStoreRequests = new SortedList<ID, KademliaNode.OutstandingStoreRequest>();
			responseCache = new SortedList<ID, KademliaNode.CachedResponse>();
            responseCacheLocker = new AutoResetEvent(true);
			lastReplication = default(DateTime);

			// Start minding the buckets
			bucketMinder = new Thread(new ThreadStart(MindBuckets));
			bucketMinder.IsBackground = true;
			bucketMinder.Start();
			
			// Start minding the conversation state caches
			authMinder = new Thread(new ThreadStart(MindCaches));
			authMinder.IsBackground = true;
			authMinder.Start();

			// Start maintainance
			maintainanceMinder = new Thread(new ThreadStart(MindMaintainance));
			maintainanceMinder.IsBackground = true;
			maintainanceMinder.Start();
		}
		
		/// <summary>
		/// Bootstrap by pinging the self node endpoint. It is a hack to bootstrap the first node without
        /// single boot. Returns true if there are no errors.
		/// </summary>
		/// <returns>True if there are no errors, false otherwise.</returns>
		public bool Bootstrap()
		{
			return Bootstrap(nodeEndpoint);
		}
			
		/// <summary>
		/// Bootstrap the node by having it ping another node. Returns true if we get a response.
		/// </summary>
		/// <param name="other">The endpoint to ping</param>
        /// <returns>True if there are no errors, false otherwise.</returns>
		public bool Bootstrap(EndpointAddress other)
		{
			// Send a blocking ping.
			bool worked = SyncPing(other);
			Thread.Sleep(CHECK_INTERVAL); // Wait for them to notice us
			return worked;
		}

        /// <summary>
        /// Makes an asynchronous bootstrap collecting all responses and watching the cache after having
        /// sent all requests.
        /// </summary>
        /// <param name="others">List of endpoint to boorstrap</param>
        /// <returns>true if at list one node have bootstrapped; false otherwise.</returns>
        public bool AsyncBootstrap(IList<EndpointAddress> others)
        {
            Dictionary<ID, bool> conversationIds = new Dictionary<ID, bool>();
            foreach (EndpointAddress other in others)
            {
                asyncPing(other, ref conversationIds);
            }

            DateTime called = DateTime.Now;
            while (DateTime.Now < called.Add(new TimeSpan(max_time * conversationIds.Count)))
            {
                // If we got a response, send it up
                findPingResponseCache(ref conversationIds);
                Thread.Sleep(CHECK_INTERVAL); // Otherwise wait for one
            }

            foreach (ID id in conversationIds.Keys)
            {
                if (conversationIds[id])
                {
                    return true;
                }
            }
            return false;
        }
		
		/// <summary>
		/// Join the network.
		/// Assuming we have some contacts in our cache, get more by IterativeFindNoding ourselves.
		/// Then, refresh most (TODO: all) buckets.
		/// Returns true if we are connected after all that, false otherwise.
		/// </summary>
        /// <returns>true if we are connected after all that, false otherwise.</returns>
		public bool JoinNetwork() {
			log.Info("Joining network");
			IList<Contact> found = IterativeFindNode(nodeID);
			if(found == null) {
				log.Info("Found <null list>");
			} else {
				foreach(Contact c in found) {
					log.Info("Found contact: " + c.ToString());
				}
			}			
			// Should get very nearly all of them
			// RefreshBuckets(); // Put this off until first maintainance.
			if(contactCache.GetCount() > 0) {
				log.Info("Joined");
				return true;
			} else {
				log.Info("Failed to join! No other nodes known!");
				return false;
			}
		}
		#endregion
		
		#region Interface
		
		/// <summary>
		/// Returns the ID of the node
		/// </summary>
		/// <returns>An ID object representing the identificator of the node</returns>
        /// <seealso cref="Kademlia.ID"/>
		public ID GetID() 
		{
			return nodeID;
		}
		
		/// <summary>
		/// Return the port we listen on.
		/// </summary>
		/// <returns>the port where the node is listening</returns>
		public int GetPort()
		{
            return nodeEndpoint.Uri.Port;
		}
		
		/// <summary>
		/// Store something in the DHT as the original publisher.
		/// </summary>
		/// <param name="filename">
        /// The filename to analize and to put the resources obtained from into the database.
        /// </param>
		public void Put(string filename)
		{
            CompleteTag fileTag = new CompleteTag(filename);
            datastore.StoreResource(fileTag, this.transportEndpoint.Uri, DateTime.Now);
			IterativeStore(fileTag, DateTime.Now);
			// TODO: republish on suggested nodes.
		}
		
		/// <summary>
		/// Gets values for a key from the DHT. Returns the values or an empty list.
		/// </summary>
		/// <param name="key">the querystring to serach into the network</param>
		/// <returns>A list of kademlia resource from the network corresponding to the request</returns>
        /// <seealso cref="Persistence.KademliaResource"/>
		public IList<KademliaResource> Get(string key)
		{
            KademliaResource[] results = datastore.SearchFor(key);
            IList<KademliaResource> found;
            if (results.Length > 0)
            {
                found = new List<KademliaResource>(results);
            }
            else
            {
                found = new List<KademliaResource>();
            }
            IList<Contact> close;
            IterativeFindValue(key, ref found, out close);
            if (found == null)
            { // Empty list for nothing found
                return new List<KademliaResource>();
            }
            else
            {
                Dictionary<string, KademliaResource> toRet = new Dictionary<string,KademliaResource>();
                Console.WriteLine(found.Count);

                // DEBUG PRINTS
                foreach (KademliaResource kr in found)
                {
                    Console.WriteLine(" ^^^^^ Found Resources " + kr.Id);
                    foreach (DhtElement u in kr.Urls)
                    {
                        Console.WriteLine(u.Url);
                    }
                    Console.WriteLine("^^^^^");
                }
                //END

                foreach(KademliaResource el in found)
                {
                    if (toRet.ContainsKey(el.Tag.FileHash))
                    {
                        toRet[el.Tag.FileHash].MergeTo(el);
                    }
                    else
                    {
                        toRet[el.Tag.FileHash] = el;
                    }
                }

                // DEBUG PRINTS
                foreach(KademliaResource kr in toRet.Values)
                {
                    Console.WriteLine(" ^^^^^ Resource " + kr.Id);
                    foreach (DhtElement u in kr.Urls)
                    {
                        Console.WriteLine(u.Url);
                    }
                    Console.WriteLine("^^^^^");
                }
                // END

                return new List<KademliaResource>(toRet.Values);
            }
		}
		#endregion
		
		#region Maintainance Operations
		/// <summary>
		/// Expire old data, replicate all data, refresh needy buckets.
		/// </summary>
		private void MindMaintainance()
		{
            log.Info("Launched Maintenance thread!");
			while(true) {
				Thread.Sleep(MAINTAINANCE_INTERVAL);
				log.Info("Performing maintainance");
				// Expire old
                try
                {
                    datastore.Expire();
                }
                catch (Exception e)
                {
                    log.Debug("Expire not done");
                }
				//Log(datastore.GetKeys().Count + " keys stored.");
				
				// Replicate all if needed
				// We get our own lists to iterate
				if(DateTime.Now > lastReplication.Add(REPLICATE_TIME)) {
					log.Debug("Replicating data");
                    foreach (KademliaResource kr in datastore.GetAllElements())
                    {
                        foreach (Persistence.DhtElement dhtEl in kr.Urls)
                        {
                            try
                            {
                                IterativeStore(kr.Tag, (DateTime)dhtEl.Publication, new EndpointAddress(dhtEl.Url));
                            }
                            catch (Exception ex)
                            {
                                log.Error("Could not replicate", ex);
                            }
                        }
                    }
					lastReplication = DateTime.Now;
				}
				
				// Refresh any needy buckets
				RefreshBuckets();
				log.Info("Done Replication");
			}
		}
		
		/// <summary>
		/// Look for nodes to go in buckets we haven't touched in a while.
		/// </summary>
		private void RefreshBuckets()
		{
			log.Info("Refreshing buckets");
			IList<ID> toLookup = contactCache.IDsForRefresh(REFRESH_TIME);
			foreach(ID key in toLookup) {
				IterativeFindNode(key);
			}
			log.Info("Refreshed buckets");
		}
		
		#endregion
		
		#region Iterative Operations

        /// <summary>
        /// Method used to store a tag into the network pretending an address passed as parameter
        /// </summary>
        /// <param name="tag">The CompleteTag to store into the DHT</param>
        /// <param name="originalInsertion">Indicates the moment when the tag have been stored into the dht</param>
        /// <param name="endpoint">Endpoint address to associate with the tag (usually the same of the node)</param>
        private void IterativeStore(CompleteTag tag, DateTime originalInsertion, EndpointAddress endpoint = null)
        {
            IList<Contact> closest = IterativeFindNode(ID.FromString(tag.TagHash));
            log.Info("Storing at " + closest.Count + " nodes");
            foreach (Contact c in closest)
            {
                if (endpoint != null)
                {
                    Console.WriteLine("Using passed endpoint (" + endpoint + ") for Sync Store");
                    SyncStore(c, tag, originalInsertion, endpoint);
                }
                else
                {
                    Console.WriteLine("Using internal endpoint (" + nodeEndpoint + ") for Sync Store");
                    SyncStore(c, tag, originalInsertion, nodeEndpoint);
                }
            }
        }

		/// <summary>
		/// Do an iterativeFindNode operation. It is used to find nodes near the target provided.
		/// </summary>
		/// <param name="target">ID representing the target </param>
		/// <returns>A list of contact close to the target</returns>
		private IList<Contact> IterativeFindNode(ID target)
		{
            if (target != nodeID)
            {
                contactCache.Touch(target);
            }

            // Get the alpha closest nodes to the target
            // TODO: Pick the nodes from a single specific bucket
            SortedList<ID, Contact> shortlist = new SortedList<ID, Contact>();
            foreach (Contact c in contactCache.CloseContacts(PARALELLISM, target, null))
            {
                log.Info("Adding contact " + c.NodeEndPoint + " to shortlist");
                shortlist.Add(c.NodeID, c);
            }

            int shortlistIndex = 0; // Everyone before this is up.

            //ANALISYS FOR THE CLOSEST
            // Make an initial guess for the closest node
            Contact closest = null;
            foreach (Contact toAsk in shortlist.Values)
            {
                if (closest == null || (toAsk.NodeID ^ target) < (closest.NodeID ^ target))
                {
                    closest = toAsk;
                }
            }
            //END OF CLOSEST ANALISYS

            // Until we run out of people to ask or we're done...
            while (shortlistIndex < shortlist.Count && shortlistIndex < NODES_TO_FIND)
            {
                // Try the first alpha unexamined contacts
                bool foundCloser = false; // TODO: Understand what the specs wants
                Dictionary<ID, bool> conversationIds = new Dictionary<ID,bool>();
                for (int i = shortlistIndex; i < shortlistIndex + PARALELLISM && i < shortlist.Count; i++)
                {
                    asyncFindNode(shortlist.Values[i], target, ref conversationIds);
                }

                List<Contact> suggested = new List<Contact>();

                DateTime called = DateTime.Now;
                while (DateTime.Now < called.Add(new TimeSpan(max_time*conversationIds.Count)))
                {
                    // If we got a response, send it up
                    //FindNodeResponse resp = GetCachedResponse<FindNodeResponse>(question.ConversationID);
                    findNodeResponseCache(ref conversationIds, ref suggested);
                    Thread.Sleep(CHECK_INTERVAL); // Otherwise wait for one
                }

                int y = 0;
                foreach(ID id in conversationIds.Keys)
                {
                    if (! conversationIds[id])
                    {
                        // Node down. Remove from shortlist and adjust loop indicies
                        log.Info("Node is down. Removing it from shortlist!");
                        shortlist.RemoveAt(y);
                        shortlistIndex--;
                        y--;
                    }
                    y++;
                }
                // Add suggestions to shortlist and check for closest
                foreach (Contact suggestion in suggested)
                {
                    if (!shortlist.ContainsKey(suggestion.NodeID))
                    { // Contacts aren't value types so we have to do this.
                        shortlist.Add(suggestion.NodeID, suggestion);
                    }

                    //ANLISYS FOR THE CLOSEST
                    if ((suggestion.NodeID ^ target) < (closest.NodeID ^ target))
                    {
                        closest = suggestion;
                        foundCloser = true;
                    }
                    //END OF ANALISYS
                }

                shortlistIndex += PARALELLISM;
            }

            // Drop extra ones
            // TODO: This isn't what the protocol says at all.
            while (shortlist.Count > NODES_TO_FIND)
            {
                shortlist.RemoveAt(shortlist.Count - 1);
            }

            return shortlist.Values;
		}

		/// <summary>
		/// Perform a Kademlia iterativeFindValue operation.
		/// It sends out a list of strings if values are found, or null none are.
		/// </summary>
		/// <param name="target">Value to search</param>
		/// <param name="vals">Values found</param>
		/// <returns>A list of contact found near the target</returns>
		private IList<Contact> IterativeFindValue(string target, ref IList<KademliaResource> vals)
		{
			// Log the lookup
			if(ID.Hash(target) != nodeID) {
				contactCache.Touch(ID.Hash(target));
			}
			
			// Get the alpha closest nodes to the target
			// TODO: Should actually pick from a certain bucket.
			SortedList<ID, Contact> shortlist = new SortedList<ID, Contact>();
			foreach(Contact c in contactCache.CloseContacts(PARALELLISM, ID.Hash(target), null)) {
				shortlist.Add(c.NodeID, c);
			}
			
			int shortlistIndex = 0; // Everyone before this is up.

            //ANALISYS FOR THE CLOSEST
			// Make an initial guess for the closest node
			Contact closest = null;
			foreach(Contact toAsk in shortlist.Values) {
                if (closest == null || (toAsk.NodeID ^ ID.Hash(target)) < (closest.NodeID ^ ID.Hash(target)))
                {
					closest = toAsk;
				}
			}
            //END OF ANALISYS
			
			// Until we run out of people to ask or we're done...
			while(shortlistIndex < shortlist.Count && shortlistIndex < NODES_TO_FIND) {
				// Try the first alpha unexamined contacts
                bool foundCloser = false; // TODO: Understand what the specs wants
                Dictionary<ID, bool> conversationIds = new Dictionary<ID,bool>();
				for(int i = shortlistIndex; i < shortlistIndex + PARALELLISM && i < shortlist.Count; i++) {
					asyncFindValue(shortlist.Values[i], target, ref conversationIds);
                }

                List<Contact> suggested = new List<Contact>();

                DateTime called = DateTime.Now;
			    while(DateTime.Now < called.Add(new TimeSpan(max_time*conversationIds.Count))) {
				    // See if we got data!
				    findDataResponseCache(ref conversationIds, ref vals);
				    // If we got a contact, send it up
				    findContactResponseCache(ref conversationIds, ref suggested);
				    Thread.Sleep(CHECK_INTERVAL); // Otherwise wait for one
			    }

                int y = 0;
                foreach (ID id in conversationIds.Keys)
                {
                    if (!conversationIds[id])
                    {
                        // Node down. Remove from shortlist and adjust loop indicies
                        log.Info("Node is down. Removing it from shortlist!");
                        shortlist.RemoveAt(y);
                        y--;
                        shortlistIndex--;
                    }
                    y++;
                }
				// But first, we have to store it at the closest node that doesn't have it yet.
				// TODO: Actually do that. Not doing it now since we don't have the publish time.
				return shortlist.Values;
			}
			
			// Drop extra ones
			// TODO: Adjust it to map better the protocol
			while(shortlist.Count > NODES_TO_FIND) {
				shortlist.RemoveAt(shortlist.Count - 1);
			}
			return shortlist.Values;
		}

        /// <summary>
        /// Do an iterativeFindValue.
        /// If we find values, we return them and put null in close.
        /// If we don't, we return null and put a list of close nodes in close.
        /// </summary>
        /// <param name="target">The value to find</param>
        /// <param name="found">The reference of a list of KademliaResource found</param>
        /// <param name="close">The output parameter to return close contacts</param>
        private void IterativeFindValue(string target, ref IList<KademliaResource> found, out IList<Contact> close)
        {
            close = IterativeFindValue(target, ref found);
        }
		#endregion
		
		#region Synchronous Operations
		/// <summary>
		/// Try to store the tag passed to the given node.
		/// </summary>
		/// <param name="storeAt">The node where the resource have to be stored</param>
		/// <param name="tag">The tag to store</param>
		/// <param name="originalInsertion">Datetime indicating when the tag have been stored</param>
        /// <param name="endpoint">Endpoint of the original owner of the resource</param>
		private void SyncStore(Contact storeAt, CompleteTag tag, DateTime originalInsertion, EndpointAddress endpoint)
		{
			// Make a message
            ID tagID = ID.FromString(tag.TagHash);
			StoreQuery storeIt = new StoreQuery(nodeID, tagID, originalInsertion, endpoint.Uri);
			
			// Record having sent it
			OutstandingStoreRequest req = new OutstandingStoreRequest();
            req.key = tagID;
			req.val = tag;
			req.sent = DateTime.Now;
			req.publication = originalInsertion;
			lock(sentStoreRequests) {
				sentStoreRequests[storeIt.ConversationID] = req;
			}
			
			// Send it
            IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                new NetUdpBinding(), new EndpointAddress(storeAt.NodeEndPoint)
            );
            svc.HandleStoreQuery(storeIt);
		}
		
		/// <summary>
		/// Send a FindNode request asynchronously and adds it to the structure passed.
		/// </summary>
		/// <param name="ask">The contact to ask</param>
		/// <param name="toFind">The resource (in this case a node) to find</param>
        /// <param name="conversationIds">A reference to structure that is filled with new question</param>
		private void asyncFindNode(Contact ask, ID toFind, ref Dictionary<ID, bool> conversationIds)
		{
			// Send message

			FindNode question = new FindNode(nodeID, toFind, nodeEndpoint.Uri);
            IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                new NetUdpBinding(), new EndpointAddress(ask.NodeEndPoint)
            );
            svc.HandleFindNode(question);

            conversationIds[question.ConversationID] = false;
		}
		
		/// <summary>
		/// Send an asynchronous FindValue and put the question done on the network in the structure passed.
		/// </summary>
		/// <param name="ask">The contact to ask</param>
		/// <param name="toFind">The value to find</param>
        /// <param name="conversationIds">A reference to structure that is filled with new question</param>
		private void asyncFindValue(Contact ask, string toFind, ref Dictionary<ID, bool> conversationIds)
		{
			// Send message
			FindValue question = new FindValue(nodeID, toFind, nodeEndpoint.Uri);
            IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                new NetUdpBinding(), new EndpointAddress(ask.NodeEndPoint)
            );
            svc.HandleFindValue(question);

            conversationIds[question.ConversationID] = false;
        }

        /// <summary>
        /// Method used to send a ping asynchronously to a recipient and store the ping into a passed structure.
        /// </summary>
        /// <param name="toPing">Node to ping</param>
        /// <param name="conversationIds">A reference to structure that is filled with new question</param>
        private void asyncPing(EndpointAddress toPing, ref Dictionary<ID, bool> conversationIds)
        {
            // Send message
            Ping ping = new Ping(nodeID, nodeEndpoint.Uri);
            IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                new NetUdpBinding(), toPing
            );
            svc.HandlePing(ping);

            conversationIds[ping.ConversationID] = false;
        }

		/// <summary>
		/// Send a ping and wait for a response or a timeout.
		/// </summary>
		/// <param name="toPing">The node to whom send the message</param>
		/// <returns>true on a response, false otherwise</returns>
		private bool SyncPing(EndpointAddress toPing)
		{
			// Send message
			Ping ping = new Ping(nodeID, nodeEndpoint.Uri);
            IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                new NetUdpBinding(), toPing
            );
            svc.HandlePing(ping);

            DateTime called = DateTime.Now;
			while(DateTime.Now < called.Add(MAX_SYNC_WAIT)) {
				// If we got a response, send it up
				Pong resp = GetCachedResponse<Pong>(ping.ConversationID);
				if(resp != null) {
					return true; // They replied in time
				}
				Thread.Sleep(CHECK_INTERVAL); // Otherwise wait for one
			}
			log.Info("Ping timeout");
			return false; // Nothing in time
		}
		#endregion

        #region Events Delegates
        /// <summary>
        /// Delegate for the method HandlePing. This method will send a pong to the requestor.
        /// </summary>
        /// <param name="o">An instance of the Ping message</param>
        private void handlePingDelegate(Object o)
        {
            Ping ping = (Ping)o;
            HandleMessage(ping);
            Console.WriteLine("Handling ping from: " + ping.NodeEndpoint);
            Pong pong = new Pong(nodeID, ping, nodeEndpoint.Uri);
            IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                new NetUdpBinding(), new EndpointAddress(ping.NodeEndpoint)
            );
            svc.HandlePong(pong);
        }

        /// <summary>
        /// Delegate for method HandlePong. This method capture and caches the pong message.
        /// </summary>
        /// <param name="o">A pong message object</param>
        private void handlePongDelegate(Object o)
        {
            Pong pong = (Pong)o;
            Console.WriteLine("Handling pong from: " + pong.NodeEndpoint);
            CacheResponse(pong);
        }

        /// <summary>
        /// Delegate for method HandleFindNode. It search a node that is found to the target and the send
        /// a response to the sender.
        /// </summary>
        /// <param name="o">A FindNode message object</param>
        private void handleFindNodeDelegate(Object o)
        {
            FindNode request = (FindNode)o;
            HandleMessage(request);
            List<Contact> closeNodes = contactCache.CloseContacts(request.Target, request.SenderID);
            FindNodeResponse response = new FindNodeResponse(nodeID, request, closeNodes, nodeEndpoint.Uri);
            IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                new NetUdpBinding(), new EndpointAddress(request.NodeEndpoint)
            );
            svc.HandleFindNodeResponse(response);
        }

        /// <summary>
        /// Delegate method of HandleFindNodeResponse. It receive the FindNodeResponse and cache it.
        /// </summary>
        /// <param name="o">A FindNodeResponse object</param>
        private void handleFindNodeResponseDelegate(Object o)
        {
            FindNodeResponse response = (FindNodeResponse)o;
            CacheResponse(response);
        }

        /// <summary>
        /// Delegate method of HandleFindValue. It receive the FindValue and checks if it is possible to
        /// find it locally. If so, it sends back a FindValueDataResponse, otherwise it sends back a
        /// FindValueContactResponse with contact that may better cover te search.
        /// </summary>
        /// <param name="o">A FindValue message</param>
        private void handleFindValueDelegate(Object o)
        {
            FindValue request = (FindValue)o;
            HandleMessage(request);
            log.Info("Searching for: " + request.Key);
            KademliaResource[] results = datastore.SearchFor(request.Key);
            if (results.Length > 0)
            {
                log.Info("Sending data to requestor: " + request.NodeEndpoint.ToString());
                FindValueDataResponse response = new FindValueDataResponse(nodeID, request, results, nodeEndpoint.Uri);
                IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                    new NetUdpBinding(), new EndpointAddress(request.NodeEndpoint)
                );
                svc.HandleFindValueDataResponse(response);
            }
            else
            {
                List<Contact> closeNodes = contactCache.CloseContacts(ID.Hash(request.Key), request.SenderID);
                FindValueContactResponse response = new FindValueContactResponse(nodeID, request, closeNodes, nodeEndpoint.Uri);
                IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                    new NetUdpBinding(), new EndpointAddress(request.NodeEndpoint)
                );
                svc.HandleFindValueContactResponse(response);
            }
        }

        /// <summary>
        /// Delegate method for HandleStoreQuery that checks if we already have the resource saved locally.
        /// If so, the resource is refreshed, otherwise an HandleStoreResponse is sent to the requestor.
        /// </summary>
        /// <param name="o">A StoreQuery response</param>
        private void handleStoreQueryDelegate(Object o)
        {
            StoreQuery request = (StoreQuery)o;
            HandleMessage(request);

            if (!datastore.ContainsUrl(request.TagHash.ToString(), request.NodeEndpoint))
            {
                acceptedStoreRequests[request.ConversationID] = DateTime.Now; // Record that we accepted it
                StoreResponse response = new StoreResponse(nodeID, request, true, nodeEndpoint.Uri);
                IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                    new NetUdpBinding(), new EndpointAddress(request.NodeEndpoint)
                );
                svc.HandleStoreResponse(response);
            }
            else if (request.PublicationTime > datastore.GetPublicationTime(request.TagHash.ToString(), request.NodeEndpoint)
                    && request.PublicationTime < DateTime.Now.ToUniversalTime().Add(MAX_CLOCK_SKEW))
            {
                datastore.RefreshResource(request.TagHash.ToString(), request.NodeEndpoint, request.PublicationTime);
            }
        }

        /// <summary>
        /// Delegate method for HandleStoreData; it checks if we have accepted the StoreQuery and, if so,
        /// the data is stored into the datastore.
        /// </summary>
        /// <param name="o">A StoreData message containing the data</param>
        private void handleStoreDataDelegate(Object o)
        {
            StoreData request = (StoreData)o;
            HandleMessage(request);
            // If we asked for it, store it and clear the authorization.
            lock (acceptedStoreRequests)
            {
                if (acceptedStoreRequests.ContainsKey(request.ConversationID))
                {
                    //acceptedStoreRequests.Remove(request.Key);
                    acceptedStoreRequests.Remove(request.ConversationID);

                    // TODO: Calculate when we should expire this data according to Kademlia
                    // For now just keep it until it expires

                    // Don't accept stuff published far in the future
                    if (request.PublicationTime < DateTime.Now.ToUniversalTime().Add(MAX_CLOCK_SKEW))
                    {
                        // We re-hash since we shouldn't trust their hash
                        Console.WriteLine("Arrived store data from peer with transport: " + request.TransportUri);
                        datastore.StoreResource(request.Data, request.TransportUri, request.PublicationTime);
                    }
                }
            }
        }

        /// <summary>
        /// Delegate method for HandleStoreResponse. Received the message, this method sends back
        /// the data to store (if requested into the StoreResponse).
        /// </summary>
        /// <param name="o">The StoreResponse passed; indicating wheter to send back or not the data</param>
        private void handleStoreResponseDelegate(Object o)
        {
            StoreResponse response = (StoreResponse)o;
            CacheResponse(response);
            lock (sentStoreRequests)
            {
                if (response.ShouldSendData
                   && sentStoreRequests.ContainsKey(response.ConversationID))
                {
                    // We actually sent this
                    // Send along the data and remove it from the list
                    OutstandingStoreRequest toStore = sentStoreRequests[response.ConversationID];
                    StoreData r = new StoreData(nodeID, response, toStore.val, toStore.publication, nodeEndpoint.Uri, transportEndpoint.Uri);
                    Console.WriteLine("Transport Endpoint transmitted => " + r.TransportUri);
                    IKademliaNode svc = ChannelFactory<IKademliaNode>.CreateChannel(
                        new NetUdpBinding(), new EndpointAddress(response.NodeEndpoint)
                    );
                    svc.HandleStoreData(r);
                    sentStoreRequests.Remove(response.ConversationID);
                }
            }
        }

        /// <summary>
        /// Method used to handle a FindValueContactResponse. This method simply caches the response.
        /// </summary>
        /// <param name="o">A FindValueContactResponse from the interroged node</param>
        private void handleFindValueContactResponseDelegate(Object o)
        {
            FindValueContactResponse response = (FindValueContactResponse)o;
            CacheResponse(response);
        }

        /// <summary>
        /// Method used to handle a FindValueDataResponse. This method simply stores the data in cache.
        /// </summary>
        /// <param name="o">A FindValueDataResponse containig the data to store</param>
        private void handleFindValueDataResponseDelegate(Object o)
        {
            FindValueDataResponse response = (FindValueDataResponse)o;
            CacheResponse(response);
        }
        #endregion

        #region Protocol Events
        /// <summary>
		/// Record every contact we see in our cache, if applicable. 
		/// </summary>
		/// <param name="msg">The message received</param>
		public void HandleMessage(Message msg)
		{
			log.Info(nodeID.ToString() + " got " + msg.Name + " from " + msg.SenderID.ToString());
			SawContact(new Contact(msg.SenderID,msg.NodeEndpoint));
		}
		
		/// <summary>
		/// Store responses in the response cache to be picked up by threads waiting for them
		/// </summary>
		/// <param name="response">The response to cache</param>
		public void CacheResponse(Response response)
		{
            HandleMessage(response);
            log.Info("Caching response");
			CachedResponse entry = new CachedResponse();
			entry.arrived = DateTime.Now;
			entry.response = response;
			//Log("Caching " + response.GetName() + " under " + response.GetConversationID().ToString());
			// Store in cache
            responseCacheLocker.WaitOne();
		    responseCache[response.ConversationID] = entry;
			responseCacheLocker.Set();
		}
		
		/// <summary>
		/// Respond to a ping by sending a pong
		/// </summary>
		/// <param name="ping">The ping message handled</param>
		public void HandlePing(Ping ping)
		{
            ThreadPool.QueueUserWorkItem(new WaitCallback(handlePingDelegate), ping);
		}

        /// <summary>
        /// Handles a pong message
        /// </summary>
        /// <param name="pong">The pong message to handle</param>
        public void HandlePong(Pong pong)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(handlePongDelegate), pong);
        }
		
		/// <summary>
		/// Send back the contacts for the K closest nodes to the desired ID, not including the requestor.
		/// K = BucketList.BUCKET_SIZE;
		/// </summary>
		/// <param name="request">The FindNode request found</param>
		public void HandleFindNode(FindNode request)
		{
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleFindNodeDelegate), request);
		}

        /// <summary>
        /// The method sed to handle a FindNodeResponse.
        /// </summary>
        /// <param name="response">The FindNodeResponse message</param>
        public void HandleFindNodeResponse(FindNodeResponse response)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleFindNodeResponseDelegate), response);
        }
		
		/// <summary>
		/// Give the value if we have it, or the closest nodes if we don't.
		/// </summary>
		/// <param name="request">The FindValue request originating the search process</param>
		public void HandleFindValue(FindValue request)
		{
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleFindValueDelegate), request);
		}
		
		/// <summary>
		/// Ask for data if we don't already have it. Update time info if we do.
		/// </summary>
		/// <param name="request">The StoreQuery object that requires value storing</param>
		public void HandleStoreQuery(StoreQuery request)
		{
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleStoreQueryDelegate), request);
		}
		
		/// <summary>
		/// Store the data, if we requested it.
		/// </summary>
		/// <param name="request">The StoreData message containig the data to store</param>
		public void HandleStoreData(StoreData request)
		{
            Console.WriteLine("Arrived a storeData from " + request.TransportUri);
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleStoreDataDelegate), request);
		}
		
		/// <summary>
		/// Send data in response to affirmative SendResponses
		/// </summary>
		/// <param name="response">The response to indicates wheter we can or not store</param>
		public void HandleStoreResponse(StoreResponse response)
		{
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleStoreResponseDelegate), response);
		}
		
        /// <summary>
        /// The method used to return the list of contact that could match a request.
        /// </summary>
        /// <param name="response">The FindValueContactResponse that contains the list</param>
        public void HandleFindValueContactResponse(FindValueContactResponse response)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleFindValueContactResponseDelegate), response);
        }

        /// <summary>
        /// The method used to recover information required by a FindValue message
        /// </summary>
        /// <param name="response">The message containing the Data</param>
        public void HandleFindValueDataResponse(FindValueDataResponse response)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleFindValueDataResponseDelegate), response);
        }
		
		#endregion

        #region Cache Works
        /// <summary>
        /// Method that, passed a dictionary representing conversations (to whom we have done a request) and a
        /// list of values to fill, checks if the specific contact have sent some data and then it sets
        /// the contact to true and add data to the list.
        /// </summary>
        /// <param name="toSearch">Conversations to search the responses from</param>
        /// <param name="vals">List to fill</param>
        private void findContactResponseCache(ref Dictionary<ID, bool> toSearch, ref List<Contact> vals)
        {
            log.Debug("Searching for contact in cache!");
            responseCacheLocker.WaitOne();
            List<ID> keys = new List<ID>(toSearch.Keys);
            for (int i = 0; i < toSearch.Count; i++)
            {
                ID cId = keys[i];
                if (responseCache.ContainsKey(cId))
                {
                    try
                    {
                        toSearch[cId] = true;
                        foreach (Contact c in ((FindValueContactResponse)responseCache[cId].response).Contacts)
                        {
                            vals.Add(c);
                        }
                        responseCache.Remove(cId);
                    }
                    catch (Exception) { }
                }
            }
            responseCacheLocker.Set();
        }

        /// <summary>
        /// Method that, passed a dictionary representing conversations (to whom we have done a request) and a
        /// list of values to fill, checks if the specific contact have sent some data and then it sets
        /// the contact to true and add data to the list.
        /// </summary>
        /// <param name="toSearch">Conversations to search the responses from</param>
        /// <param name="vals">List to fill</param>
        private void findDataResponseCache(ref Dictionary<ID, bool> toSearch, ref IList<KademliaResource> vals)
        {
            log.Debug("Searching for data in cache!");
            responseCacheLocker.WaitOne();
            List<ID> keys = new List<ID>(toSearch.Keys);
            for (int i = 0; i < toSearch.Count; i++)
            {
                ID cId = keys[i];
                if (responseCache.ContainsKey(cId))
                {
                    try
                    {
                        toSearch[cId] = true;
                        foreach (KademliaResource c in ((FindValueDataResponse)responseCache[cId].response).Values)
                        {
                            vals.Add(c);
                            Console.WriteLine("Found resource " + c.Id);
                        }
                        responseCache.Remove(cId);
                    }
                    catch (Exception) { }
                }
            }
            responseCacheLocker.Set();
        }

        /// <summary>
        /// Method that, passed a dictionary representing conversations (to whom we have done a request) checks if the specific contact have sent some data and then it sets
        /// the contact to true.
        /// </summary>
        /// <param name="toSearch">Conversations to search the responses from</param>
        private void findPingResponseCache(ref Dictionary<ID, bool> toSearch)
        {
            responseCacheLocker.WaitOne();
            List<ID> keys = new List<ID>(toSearch.Keys);
            for(int i = 0; i< toSearch.Count; i++)
            {
                ID cID = keys[i];
                if (responseCache.ContainsKey(cID))
                {
                    toSearch[cID] = true;
                    responseCache.Remove(cID);
                }
            }
            responseCacheLocker.Set();
        }

        /// <summary>
        /// Method that, passed a dictionary representing conversations (to whom we have done a request) and a
        /// list of values to fill, checks if the specific contact have sent some data and then it sets
        /// the contact to true and add data to the list.
        /// </summary>
        /// <param name="toSearch">Conversations to search the responses from</param>
        /// <param name="suggested">List to fill</param>
        private void findNodeResponseCache(ref Dictionary<ID, bool> toSearch, ref List<Contact> suggested)
        {
            responseCacheLocker.WaitOne();
            List<ID> keys = new List<ID>(toSearch.Keys);
            for (int i = 0; i < toSearch.Count; i++)
            {
                ID cId = keys[i];
                if (responseCache.ContainsKey(cId))
                {
                    try
                    {
                        toSearch[cId] = true;
                        foreach (Contact c in ((FindNodeResponse)responseCache[cId].response).Contacts)
                        {
                            suggested.Add(c);
                        }
                        responseCache.Remove(cId);
                    }
                    catch (Exception) { }
                }
            }
            responseCacheLocker.Set();
        }

        /// <summary>
        /// Get a properly typed response from the cache, or null if none exists.
        /// </summary>
        /// <param name="conversation">The conversation to search</param>
        /// <returns>The value found typed by T</returns>
        private T GetCachedResponse<T>(ID conversation) where T : Response
        {
            responseCacheLocker.WaitOne();
            if (responseCache.ContainsKey(conversation))
            { // If we found something of the right type
                try
                {
                    T toReturn = (T)responseCache[conversation].response;
                    responseCache.Remove(conversation);
                    //Log("Retrieving cached " + toReturn.GetName());
                    responseCacheLocker.Set();
                    return toReturn; // Pull it out and return it
                }
                catch (Exception ex)
                {
                    // Couldn't actually cast to type we want.
                    responseCacheLocker.Set();
                    return null;
                }
            }
            else
            {
                //Log("Nothing for " + conversation.ToString());
                responseCacheLocker.Set();
                return null; // Nothing there -> null
            }
        }

        /// <summary>
        /// Expire entries in the accepted/sent store request caches and the response cache.
        /// </summary>
        private void MindCaches()
        {
            log.Info("Starting cache manager");
            while (true)
            {
                // Do accepted requests
                lock (acceptedStoreRequests)
                {
                    for (int i = 0; i < acceptedStoreRequests.Count; i++)
                    {
                        // Remove stuff that is too old
                        if (DateTime.Now.Subtract(acceptedStoreRequests.Values[i]) > MAX_CACHE_TIME)
                        {
                            acceptedStoreRequests.RemoveAt(i);
                            i--;
                        }
                    }
                }

                // Do sent requests
                lock (sentStoreRequests)
                {
                    for (int i = 0; i < sentStoreRequests.Count; i++)
                    {
                        if (DateTime.Now.Subtract(sentStoreRequests.Values[i].sent) > MAX_CACHE_TIME)
                        {
                            sentStoreRequests.RemoveAt(i);
                            i--;
                        }
                    }
                }

                // Do responses
                responseCacheLocker.WaitOne();
                for (int i = 0; i < responseCache.Count; i++)
                {
                    if (DateTime.Now.Subtract(responseCache.Values[i].arrived) > MAX_CACHE_TIME)
                    {
                        responseCache.RemoveAt(i);
                        i--;
                    }
                }
                responseCacheLocker.Set();

                Thread.Sleep(CHECK_INTERVAL);
            }
        }
        #endregion

        #region Buckets

        /// <summary>
		/// Call this whenever we see a contact.
		/// We add the contact to the queue to be cached.
		/// </summary>
		/// <param name="seen">The contact seen</param>
		private void SawContact(Contact seen)
		{
            if (seen.NodeID == nodeID)
            {
				return; // NEVER insert ourselves!
			}
			
			lock(contactQueue) {
				if(contactQueue.Count < MAX_QUEUE_LENGTH) { // Don't let it get too long
					contactQueue.Add(seen);
				}
				
			}
		}
		
		/// <summary>
		/// Run in the background and add contacts to the cache.
		/// </summary>
		private void MindBuckets()
		{
            log.Info("Starting buckets periodic manager.");
			while(true) {
				
				// Handle all the queued contacts
				while(contactQueue.Count > 0) {
					Contact applicant;
					lock(contactQueue) { // Only lock when getting stuff.
						applicant = contactQueue[0];
						contactQueue.RemoveAt(0);
					}
					
					//Log("Processing contact for " + applicant.GetID().ToString());
					
					// If we already know about them
                    if (contactCache.Contains(applicant.NodeID))
                    {
						// If they have a new address, record that
                        if (contactCache.Get(applicant.NodeID).NodeEndPoint != applicant.NodeEndPoint) 
                        {
							// Replace old one
                            contactCache.Remove(applicant.NodeID);
							contactCache.Put(applicant);
						}
                        else // Just promote them
                        { 
                            contactCache.Promote(applicant.NodeID);
						}
						continue;
					}
					
					// If we can fit them, do so
					Contact blocker = contactCache.Blocker(applicant.NodeID);
					if(blocker == null) {
						contactCache.Put(applicant);
					} else {
						// We can't fit them. We have to choose between blocker and applicant
						if(!SyncPing(new EndpointAddress(blocker.NodeEndPoint))) { // If the blocker doesn't respond, pick the applicant.
                            contactCache.Remove(blocker.NodeID);
							contactCache.Put(applicant);
							log.Info("Choose applicant");
						} else {
							log.Info("Choose blocker");
						}
					}
					
					//Log(contactCache.ToString());
				}
				
				// Wait for more
				Thread.Sleep(CHECK_INTERVAL);
			}
		}
		#endregion
		
	}
}
