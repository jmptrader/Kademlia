using System;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
    public abstract class BaseRouter
    {
#if DEBUG       // for unit testing
        public List<Contact> CloserContacts { get; protected set; }
        public List<Contact> FartherContacts { get; protected set; }
#endif

        public Node Node { get { return node; } set { node = value; } }

        protected Node node;

        public abstract (bool found, List<Contact> contacts, Contact foundBy, string val) Lookup(
            ID key,
            Func<ID, List<Contact>, (List<Contact> contacts, Contact foundBy, string val)> rpcCall,
            bool giveMeAll = false);

        public abstract (List<Contact> contacts, Contact foundBy, string val) RpcFindValue(ID key, List<Contact> contacts);
        public abstract (List<Contact> contacts, Contact foundBy, string val) RpcFindNodes(ID key, List<Contact> contacts);

        /// <summary>
        /// Using the k-bucket's key (it's high value), find the closest 
        /// k-bucket the given key that isn't empty.
        /// </summary>
#if DEBUG           // For unit testing.
        public virtual KBucket FindClosestNonEmptyKBucket(ID key)
#else
        protected virtual KBucket FindClosestNonEmptyKBucket(ID key)
#endif
        {
            KBucket closest = node.BucketList.Buckets.Where(b => b.Contacts.Count > 0).OrderBy(b => b.Key ^ key).FirstOrDefault();
            Validate.IsTrue<NoNonEmptyBucketsException>(closest != null, "No non-empty buckets exist.  You must first register a peer and add that peer to your bucketlist.");

            return closest;
        }

        /// <summary>
        /// Get sorted list of closest nodes to the given key.
        /// </summary>
#if DEBUG           // For unit testing.
        public List<Contact> GetClosestNodes(ID key, KBucket bucket)
#else
        protected List<Contact> GetClosestNodes(ID key, KBucket bucket)
#endif
        {
            return bucket.Contacts.OrderBy(c => c.ID ^ key).ToList();
        }
    }
}
