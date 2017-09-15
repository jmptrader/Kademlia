using System.Collections.Generic;
using System.Linq;

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

        public Dht(ID id, IProtocol protocol, IStorage storage)
        {
            this.storage = storage;
            ourId = id;
            ourContact = new Contact(protocol, id);
            node = new Node(ourContact, storage);
            router = new Router(node);
        }

        /// <summary>
        /// Bootstrap our peer by contacting another peer, adding its contacts
        /// to our list, then getting the contacts for other peers not in the
        /// bucket range of our known peer we're joining.
        /// </summary>
        /// <param name="knownPeer"></param>
        public void Bootstrap(Contact knownPeer)
        {
            node.BucketList.AddContact(knownPeer);
            List<Contact> contacts = knownPeer.Protocol.FindNode(ourContact, ourId);
            contacts.ForEach(c => node.BucketList.AddContact(c));
            KBucket knownPeerBucket = node.BucketList.Buckets.Single(b => b.HasInRange(knownPeer.ID));
            // Resolve the list now, so we don't include additional contacts as we add to our bucket additional contacts.
            var otherBuckets = node.BucketList.Buckets.Where(b => b != knownPeerBucket);

            foreach (KBucket otherBucket in otherBuckets)
            {
                ID rndId = ID.RandomIDWithinBucket(otherBucket);
                List<Contact> contactsFurtherAway = otherBucket.Contacts.ToList();
                contactsFurtherAway.ForEach(c => c.Protocol.FindNode(ourContact, rndId).ForEach(otherContact => node.BucketList.AddContact(otherContact)));
            }
        }

        public void Store(ID key, string val)
        {
            // We're storing to ourselves as well as k closer contacts.
            storage.Set(key, val);
            List<Contact> contacts = router.Lookup(key, router.RpcFindNodes).contacts;
            contacts.ForEach(c => c.Protocol.Store(node.OurContact, key, val));
        }

        public (bool found, List<Contact> contacts, string val) FindValue(ID key)
        {
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
                ret = LookupValue(key);

                if (ret.found)
                {
                    node.Cache(key, ret.val);
                }
            }

            return ret;
        }

#if DEBUG       // For unit testing
        public (bool found, List<Contact> contacts, string val) LookupValue(ID key)
#else
        protected (bool found, List<Contact> contacts, string val) LookupValue(ID key)
#endif
        {
            var (contacts, val) = router.Lookup(key, router.RpcFindValue);
            var found = contacts == null;

            return (found, contacts, val);
        }
    }
}
