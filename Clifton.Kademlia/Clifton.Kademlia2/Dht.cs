using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Clifton.Kademlia
{
    public class Dht
    {
#if DEBUG       // for unit testing
        public Router Router { get { return router; } }
#endif

        protected Router router;
        protected IStorage storage;
        protected IProtocol protocol;
        protected Node node;
        protected Contact ourContact;
        protected ID ourId;
        protected Timer bucketRefreshTimer;
        protected Timer keyValueRepublishTimer;

        public Dht(ID id, IProtocol protocol, IStorage storage)
        {
            this.storage = storage;
            ourId = id;
            ourContact = new Contact(protocol, id);
            node = new Node(ourContact, storage);
            router = new Router(node);
            SetupBucketRefreshTimer();
            SetupKeyValueRepublishTimer();
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

            // We're storing to ourselves as well as k closer contacts.
            storage.Set(key, val);
			StoreOnCloserContacts(key, val);
        }

        public (bool found, List<Contact> contacts, string val) FindValue(ID key)
        {
            TouchBucketWithKey(key);

            string ourVal;
            List<Contact> contactsQueried = new List<Contact>();
            (bool found, List<Contact> contacts, string val) ret = (false, null, null);

            // If we have it, return with our value.
            if (storage.TryGetValue(key, out ourVal))
            {
                ret = (true, null, ourVal);
            }
            else
            {
                var lookup = router.Lookup(key, router.RpcFindValue);

                if (lookup.found)
                {
                    ret = (true, null, lookup.val);
                    // Find the first close contact (other than the one the value was found by) in which to also store the key-value.
                    var storeTo = lookup.contacts.Where(c => c != lookup.foundBy).OrderBy(c => c.ID.Value ^ key.Value).FirstOrDefault();

                    if (storeTo != null)
                    {
                        storeTo.Protocol.Store(node.OurContact, key, lookup.val);
                    }
                }
            }

            return ret;
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

			node.Storage.Where(k => (now - storage.GetTimeStamp(k)).TotalMilliseconds >= Constants.KEY_VALUE_REPUBLISH_INTERVAL).ForEach(k=>
			{
                ID key = new ID(k);
				StoreOnCloserContacts(key, storage.Get(key));
				storage.Touch(k);			
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
