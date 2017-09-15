// #define TRY_CLOSEST_BUCKET

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Clifton.Kademlia
{
    public class Router
    {
#if DEBUG       // for unit testing
        public Node Node { get { return node; } }
        public List<Contact> CloserContacts { get; protected set; }
        public List<Contact> FartherContacts { get; protected set; }
#endif

        protected Node node;

        public Router(Node node)
        {
            this.node = node;
        }

        public virtual (List<Contact> contacts, string val) Lookup(
            ID key, 
            Func<ID, List<Contact>, (List<Contact> contacts, string val)> rpcCall, 
            bool giveMeAll = false)
        {
            bool haveWork = true;
            List<Contact> ret = new List<Contact>();
            List<Contact> contactedNodes = new List<Contact>();
            List<Contact> closerContacts = new List<Contact>();
            List<Contact> fartherContacts = new List<Contact>();
            string val = null;

#if TRY_CLOSEST_BUCKET
            // Spec: The lookup initiator starts by picking a nodes from its closest non-empty k-bucket
            KBucket bucket = FindClosestNonEmptyKBucket(key);

            // Not in spec -- sort by the closest nodes in the closest bucket.
            List<Contact> allNodes = node.BucketList.GetCloseContacts(key, node.OurContact.ID).Take(Constants.K).ToList(); 
            List<Contact> nodesToQuery = allNodes.Take(Constants.ALPHA).ToList();
            fartherContacts.AddRange(allNodes.Skip(Constants.ALPHA).Take(Constants.K - Constants.ALPHA));
#else
#if DEBUG
            List<Contact> allNodes = node.BucketList.GetKBucket(key).Contacts.Take(Constants.K).ToList();
#else
            // This is a bad way to get a list of close contacts with virtual nodes because we're always going to get the closest nodes right at the get go.
            List<Contact> allNodes = node.BucketList.GetCloseContacts(key, node.OurContact.ID).Take(Constants.K).ToList(); 
#endif
            List<Contact> nodesToQuery = allNodes.Take(Constants.ALPHA).ToList();

            // Also not explicitly in spec:
            // Any closer node in the alpha list is immediately added to our closer contact list, and
            // any farther node in the alpha list is immediately added to our farther contact list.
            closerContacts.AddRange(nodesToQuery.Where(n => (n.ID.Value ^ key.Value) < (node.OurContact.ID.Value ^ key.Value)));
            fartherContacts.AddRange(nodesToQuery.Where(n => (n.ID.Value ^ key.Value) >= (node.OurContact.ID.Value ^ key.Value)));

            // The remaining contacts not tested yet can be put here.
            fartherContacts.AddRange(allNodes.Skip(Constants.ALPHA).Take(Constants.K - Constants.ALPHA));
#endif

            // We're about to contact these nodes.
            contactedNodes.AddRangeDistinctBy(nodesToQuery, (a, b) => a.ID.Value == b.ID.Value);

            // Spec: The initiator then sends parallel, asynchronous FIND_NODE RPCS to the a nodes it has chosen, a is a system-wide concurrency parameter, such as 3.
            if (GetCloserNodes(key, rpcCall, nodesToQuery, closerContacts, fartherContacts, out val))
            {
                return (null, val);
            }

            // Add any new closer contacts to the list we're going to return.
            ret.AddRangeDistinctBy(closerContacts, (a, b) => a.ID.Value == b.ID.Value);

            // Spec: The lookup terminates when the initiator has queried and gotten responses from the k closest nodes it has seen.
            while (ret.Count < Constants.K && haveWork)
            {
                List<Contact> closerUncontactedNodes = closerContacts.Except(contactedNodes).ToList();
                List<Contact> fartherUncontactedNodes = fartherContacts.Except(contactedNodes).ToList();
                bool haveCloser = closerUncontactedNodes.Count > 0;
                bool haveFarther = fartherUncontactedNodes.Count > 0;

                haveWork = haveCloser || haveFarther;

                // Spec:  Of the k nodes the initiator has heard of closest to the target...
                if (haveCloser)
                {
                    // We're about to contact these nodes.
                    contactedNodes.AddRangeDistinctBy(closerUncontactedNodes, (a, b) => a.ID.Value == b.ID.Value);

                    // Spec: ...it picks a that it has not yet queried and resends the FIND_NODE RPC to them. 
                    if (GetCloserNodes(key, rpcCall, closerUncontactedNodes.Take(Constants.ALPHA).ToList(), closerContacts, fartherContacts, out val))
                    {
                        return (null, val);
                    }
                }
                else if (haveFarther)
                {
                    // We're about to contact these nodes.
                    contactedNodes.AddRangeDistinctBy(fartherUncontactedNodes, (a, b) => a.ID.Value == b.ID.Value);

                    if (GetCloserNodes(key, rpcCall, fartherUncontactedNodes, closerContacts, fartherContacts, out val))
                    {
                        return (null, val);
                    }
                }
            }

#if DEBUG       // For unit testing.
            CloserContacts = closerContacts;
            FartherContacts = fartherContacts;
#endif

            // Spec (sort of): Return max(k) closer nodes, sorted by distance.
            // For unit testing, giveMeAll can be true so that we can match against our alternate way of getting closer contacts.
            return (giveMeAll ? ret : ret.Take(Constants.K).OrderBy(c => c.ID.Value ^ key.Value).ToList(), val);
        }

        /// <summary>
        /// Get closer nodes to the current uncontacted nodes and update the list of closer and farther nodes.
        /// </summary>
#if DEBUG           // For unit testing.
        public bool GetCloserNodes(
            ID key, 
            Func<ID, List<Contact>, (List<Contact> contacts, string val)> rpcCall, 
            List<Contact> nodesToQuery, 
            List<Contact> closerContacts, 
            List<Contact> fartherContacts,
            out string val)
#else
        protected bool GetCloserNodes(
            ID key, 
            Func<ID, List<Contact>, (List<Contact> contacts, string val)> rpcCall, 
            List<Contact> nodesToQuery, 
            List<Contact> closerContacts, 
            List<Contact> fartherContacts,
            out string val)
#endif
        {
            // As in, peer's nodes:
            // Exclude ourselves and the peers we're contacting to a get unique list of new peers.
            // Compare by ID's as Contact is different instance except with a virtual network.
            var (contacts, foundVal) = rpcCall(key, nodesToQuery);
            val = foundVal;
            List<Contact> peersNodes = contacts.ExceptBy(node.OurContact, c => c.ID.Value).ExceptBy(nodesToQuery, c => c.ID.Value).ToList();

            // Null continuation is a special case primarily for unit testing when we have no nodes in any buckets.
            var nearestNodeDistance = nodesToQuery.OrderBy(n => n.ID.Value ^ key.Value).FirstOrDefault()?.ID?.Value ?? -1;

            closerContacts.
                AddRangeDistinctBy(peersNodes.
                    Where(p => (p.ID.Value ^ key.Value) < nearestNodeDistance),
                    (a, b) => a.ID.Value == b.ID.Value);

            fartherContacts.
                AddRangeDistinctBy(peersNodes.
                    Where(p => (p.ID.Value ^ key.Value) >= nearestNodeDistance),
                    (a, b) => a.ID.Value == b.ID.Value);

            return val != null;
        }

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
            KBucket closest = node.BucketList.Buckets.Where(b => b.Contacts.Count > 0).OrderBy(b => b.Key ^ key.Value).FirstOrDefault();
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
            return bucket.Contacts.OrderBy(c => c.ID.Value ^ key.Value).ToList();
        }

        /// <summary>
        /// For each contact, call the FindNode and return all the nodes whose contacts responded
        /// within a "reasonable" period of time.
        /// </summary>
        public (List<Contact> contacts, string val) RpcFindNodes(ID key, List<Contact> contacts)
        {
            List<Contact> nodes = new List<Contact>();
            contacts.ForEach(c => nodes.AddRange(c.Protocol.FindNode(node.OurContact, key)));

            return (nodes, null);
        }

        /// <summary>
        /// For each contact, call the FindNode and return all the nodes whose contacts responded
        /// within a "reasonable" period of time, unless a value is returned, at which point we stop.
        /// </summary>
        public (List<Contact> contacts, string val) RpcFindValue(ID key, List<Contact> contacts)
        {
            List<Contact> nodes = new List<Contact>();
            string retval = null;

            foreach(Contact c in contacts)
            {
                (var otherContacts, var val) = c.Protocol.FindValue(node.OurContact, key);

                if (otherContacts != null)
                {
                    nodes.AddRange(otherContacts);
                }
                else
                {
                    Validate.IsTrue<ValueCannotBeNullException>(val != null, "Null values are not supported nor expected.");
                    retval = val;
                    break;
                }
            }

            return (nodes, retval);
        }
    }
}
