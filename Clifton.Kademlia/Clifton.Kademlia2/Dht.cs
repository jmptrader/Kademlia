using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Clifton.Kademlia
{
    public class Dht
    {
#if DEBUG       // for unit testing
        public BaseRouter Router { get { return router; } }
#endif

        /// <summary>
        /// Server has access to this.
        /// </summary>
        public IStorage OriginatorStorage { get { return originatorStorage; } }

        protected BaseRouter router;
        protected IStorage originatorStorage;
        protected IStorage republishStorage;
        protected IStorage cacheStorage;
        protected IProtocol protocol;
        protected Node node;
        protected Contact ourContact;
        protected ID ourId;
        protected Timer bucketRefreshTimer;
        protected Timer keyValueRepublishTimer;
        protected Timer originatorRepublishTimer;
        protected Timer expireKeysTimer;

        /// <summary>
        /// Use this constructor to initialize the stores to the same instance.
        /// </summary>
        public Dht(ID id, IProtocol protocol, Func<IStorage> storageFactory, BaseRouter router)
        {
            originatorStorage = storageFactory();
            republishStorage = storageFactory();
            cacheStorage = storageFactory();
            FinishInitialization(id, protocol, router);
        }

        /// <summary>
        /// Supports different concrete storage types.  For example, you may want the cacheStorage
        /// to be an in memory store, the originatorStorage to be a SQL database, and the republish store
        /// to be a key-value database.
        /// </summary>
        public Dht(ID id, IProtocol protocol, BaseRouter router, IStorage originatorStorage, IStorage republishStorage, IStorage cacheStorage)
        {
            this.originatorStorage = originatorStorage;
            this.republishStorage = republishStorage;
            this.cacheStorage = cacheStorage;
            FinishInitialization(id, protocol, router);
        }

/// <summary>
        /// Bootstrap our peer by contacting another peer, adding its contacts
        /// to our list, then getting the contacts for other peers not in the
        /// bucket range of our known peer we're joining.
        /// </summary>
        public void Bootstrap(Contact knownPeer)
        {
            node.BucketList.AddContact(knownPeer);
            List<Contact> contacts = knownPeer.Protocol.FindNode(ourContact, ourId);
            contacts.ForEach(c => node.BucketList.AddContact(c));
            KBucket knownPeerBucket = node.BucketList.GetKBucket(knownPeer.ID);
            // Resolve the list now, so we don't include additional contacts as we add to our bucket additional contacts.
            var otherBuckets = node.BucketList.Buckets.Where(b => b != knownPeerBucket).ToList();
            otherBuckets.ForEach(b => RefreshBucket(b));
            foreach (KBucket otherBucket in otherBuckets)
            {
                RefreshBucket(otherBucket);
            }
        }

        public void Store(ID key, string val)
        {
            TouchBucketWithKey(key);

            // We're storing to k closer contacts.
            originatorStorage.Set(key, val);
			StoreOnCloserContacts(key, val);
        }

        public (bool found, List<Contact> contacts, string val) FindValue(ID key)
        {
            TouchBucketWithKey(key);

            string ourVal;
            List<Contact> contactsQueried = new List<Contact>();
            (bool found, List<Contact> contacts, string val) ret = (false, null, null);

            if (originatorStorage.TryGetValue(key, out ourVal))
            {
                // Sort of odd that we are using the key-value store to find something the key-value that we originate.
                ret = (true, null, ourVal);
            }
            else if (republishStorage.TryGetValue(key, out ourVal))
            {
                // If we have it from another peer.
                ret = (true, null, ourVal);
            }
            else if (cacheStorage.TryGetValue(key, out ourVal))
            {
                // If we have it because it was cached.
                ret = (true, null, ourVal);
            }
            else
            {
                var lookup = router.Lookup(key, router.RpcFindValue);
                TouchBucketWithKey(key);

                if (lookup.found)
                {
                    ret = (true, null, lookup.val);
                    // Find the first close contact (other than the one the value was found by) in which to *cache* the key-value.
                    var storeTo = lookup.contacts.Where(c => c != lookup.foundBy).OrderBy(c => c.ID ^ key).FirstOrDefault();

                    if (storeTo != null)
                    {
                        int separatingNodes = GetSeparatingNodesCount(ourContact, storeTo);
                        int expTimeSec = (int)(Constants.EXPIRATION_TIME_SECONDS / Math.Pow(2, separatingNodes));
                        storeTo.Protocol.Store(node.OurContact, key, lookup.val, true, expTimeSec);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Return the number of nodes between the two contacts, where the contact list is sorted by the integer ID values (not XOR distance.)
        /// </summary>
        protected int GetSeparatingNodesCount(Contact a, Contact b)
        {
            // Sort of brutish way to do this.
            // Get all the contacts, ordered by their ID.
            List<Contact> allContacts = node.BucketList.Buckets.SelectMany(c => c.Contacts).OrderBy(c => c.ID.Value).ToList();

            int idxa = allContacts.IndexOf(a);
            int idxb = allContacts.IndexOf(b);

            return Math.Abs(idxa - idxb);
        }

        protected void FinishInitialization(ID id, IProtocol protocol, BaseRouter router)
        {
            ourId = id;
            ourContact = new Contact(protocol, id);
            node = new Node(ourContact, republishStorage, cacheStorage);
            this.router = router;
            this.router.Node = node;
            SetupBucketRefreshTimer();
            SetupKeyValueRepublishTimer();
            SetupOriginatorRepublishTimer();
            SetupExpireKeysTimer();
        }

        protected void TouchBucketWithKey(ID key)
        {
            node.BucketList.GetKBucket(key).Touch();
        }

        protected void SetupBucketRefreshTimer()
        {
            bucketRefreshTimer = new Timer(Constants.BUCKET_REFRESH_INTERVAL);
            bucketRefreshTimer.AutoReset = true;
            bucketRefreshTimer.Elapsed += BucketRefreshTimerElapsed;
            bucketRefreshTimer.Start();
        }

        protected void SetupKeyValueRepublishTimer()
        {
            keyValueRepublishTimer = new Timer(Constants.KEY_VALUE_REPUBLISH_INTERVAL);
            keyValueRepublishTimer.AutoReset = true;
            keyValueRepublishTimer.Elapsed += KeyValueRepublishElapsed;
            keyValueRepublishTimer.Start();
        }

        protected void SetupOriginatorRepublishTimer()
        {
            originatorRepublishTimer = new Timer(Constants.ORIGINATOR_REPUBLISH_INTERVAL);
            originatorRepublishTimer.AutoReset = true;
            originatorRepublishTimer.Elapsed += OriginatorRepublishElapsed;
            originatorRepublishTimer.Start();
        }

        protected void SetupExpireKeysTimer()
        {
            expireKeysTimer = new Timer(Constants.KEY_VALUE_EXPIRE_INTERVAL);
            expireKeysTimer.AutoReset = true;
            expireKeysTimer.Elapsed += ExpireKeysElapsed;
            expireKeysTimer.Start();
        }

        protected void BucketRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;

            node.BucketList.Buckets.
                Where(b => (now - b.TimeStamp).TotalMilliseconds >= Constants.BUCKET_REFRESH_INTERVAL).
                ForEach(b => RefreshBucket(b));
        }

        /// <summary>
        /// Replicate key values if the key-value hasn't been touched within the republish interval.
        /// Also don't do a FindNode lookup if the bucket containing the key has been refreshed within the refresh interval.
        /// </summary>
        protected void KeyValueRepublishElapsed(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;

			republishStorage.Where(k => (now - republishStorage.GetTimeStamp(k)).TotalMilliseconds >= Constants.KEY_VALUE_REPUBLISH_INTERVAL).ForEach(k=>
			{
                ID key = new ID(k);
				StoreOnCloserContacts(key, republishStorage.Get(key));
				republishStorage.Touch(k);			
			});
        }

        protected void OriginatorRepublishElapsed(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;

            originatorStorage.Where(k => (now - originatorStorage.GetTimeStamp(k)).TotalMilliseconds >= Constants.ORIGINATOR_REPUBLISH_INTERVAL).ForEach(k =>
            {
                ID key = new ID(k);
                // Just use close contacts, don't do a lookup.
                var contacts = node.BucketList.GetCloseContacts(key, node.OurContact.ID);
                contacts.ForEach(c => c.Protocol.Store(ourContact, key, originatorStorage.Get(key)));
                originatorStorage.Touch(k);
            });
        }

        /// <summary>
        /// Any expired keys in the republish or node's cache are removed.
        /// </summary>
        protected virtual void ExpireKeysElapsed(object sender, ElapsedEventArgs e)
        {
            RemoveExpiredData(cacheStorage);
            RemoveExpiredData(republishStorage);
        }

        protected void RemoveExpiredData(IStorage store)
        {
            DateTime now = DateTime.Now;
            // ToList so our key list is resolved now as we remove keys.
            store.Where(k => (now - store.GetTimeStamp(k)).TotalSeconds >= store.GetExpirationTimeSec(k)).ToList().ForEach(k =>
            {
                store.Remove(k);
            });
        }

        /// <summary>
        /// Perform a lookup if the bucket containing the key has not been refreshed, otherwise, just get the contacts the k closest contacts we know about.
        /// </summary>
        protected void StoreOnCloserContacts(ID key, string val)
		{
			DateTime now = DateTime.Now;

			KBucket kbucket = node.BucketList.GetKBucket(key);
			List<Contact> contacts;

			if ((now - kbucket.TimeStamp).TotalMilliseconds < Constants.BUCKET_REFRESH_INTERVAL)
			{
				// Bucket has been refreshed recently, so don't do a lookup as we have the k closes contacts.
				contacts = node.BucketList.GetCloseContacts(key, node.OurContact.ID);
			}
			else
			{
				// Do a lookup and touch the bucket since we just did a lookup.
				contacts = router.Lookup(key, router.RpcFindNodes).contacts;
				TouchBucketWithKey(key);
			}

			contacts.ForEach(c => c.Protocol.Store(node.OurContact, key, val));
		}

		protected void RefreshBucket(KBucket bucket)
        {
            bucket.Touch();
            ID rndId = ID.RandomIDWithinBucket(bucket);
            // Isolate in a separate list as contacts collection for this bucket might change.
            List<Contact> contacts = bucket.Contacts.ToList();
            contacts.ForEach(c => c.Protocol.FindNode(ourContact, rndId).ForEach(otherContact => node.BucketList.AddContact(otherContact)));
        }
    }
}
