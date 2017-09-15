// #define TRY_CLOSEST_BUCKET

using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
    public class Router
    {
#if DEBUG       // for unit testing
        public Node Node { get { return node; } }
#endif

        protected Node node;

        public Router(Node node)
        {
            this.node = node;
        }

        public virtual List<Contact> Lookup(ID key)
        {
            bool haveWork = true;
            List<Contact> ret = new List<Contact>();
            List<Contact> contactedNodes = new List<Contact>();
            List<Contact> closerContacts = new List<Contact>();
            List<Contact> fartherContacts = new List<Contact>();

#if TRY_CLOSEST_BUCKET
            // Spec: The lookup initiator starts by picking a nodes from its closest non-empty k-bucket
            KBucket bucket = FindClosestNonEmptyKBucket(key);

            // Not in spec -- sort by the closest nodes in the closest bucket.
            List<Contact> allNodes = node.BucketList.GetCloseContacts(key, node.OurContact.ID).Take(Constants.K).ToList(); 
            List<Contact> nodesToQuery = allNodes.Take(Constants.ALPHA).ToList();
            fartherContacts.AddRange(allNodes.Skip(Constants.ALPHA).Take(Constants.K - Constants.ALPHA));
#else
            List<Contact> allNodes = node.BucketList.GetCloseContacts(key, node.OurContact.ID).Take(Constants.K).ToList(); 
            List<Contact> nodesToQuery = allNodes.Take(Constants.ALPHA).ToList();
            fartherContacts.AddRange(allNodes.Skip(Constants.ALPHA).Take(Constants.K - Constants.ALPHA));
#endif

            // We're about to contact these nodes.
            contactedNodes.AddRangeDistinctBy(nodesToQuery, (a, b) => a.ID.Value == b.ID.Value);

            // Spec: The initiator then sends parallel, asynchronous FIND_NODE RPCS to the a nodes it has chosen, a is a system-wide concurrency parameter, such as 3.
            GetCloserNodes(key, nodesToQuery, closerContacts, fartherContacts);

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
                    GetCloserNodes(key, closerUncontactedNodes.Take(Constants.ALPHA).ToList(), closerContacts, fartherContacts);
                }
                else if (haveFarther)
                {
                    // We're about to contact these nodes.
                    contactedNodes.AddRangeDistinctBy(fartherUncontactedNodes, (a, b) => a.ID.Value == b.ID.Value);
                    GetCloserNodes(key, fartherUncontactedNodes, closerContacts, fartherContacts);
                }
            }

            // Spec (sort of): Return max(k) closer nodes
            return ret.Take(Constants.K).ToList();
        }

        /// <summary>
        /// Get closer nodes to the current uncontacted nodes and update the list of closer and farther nodes.
        /// </summary>
#if DEBUG           // For unit testing.
        public void GetCloserNodes(ID key, List<Contact> nodesToQuery, List<Contact> closerContacts, List<Contact> fartherContacts)
#else
        protected void GetCloserNodes(ID key, List<Contact> nodesToQuery, List<Contact> closerContacts, List<Contact> fartherContacts)
#endif
        {
            // As in, peer's nodes:
            // Exclude ourselves and the peers we're contacting to a get unique list of new peers.
            // Compare by ID's as Contact is different instance except with a virtual network.
            List<Contact> peersNodes = RpcFindNodes(key, nodesToQuery).ExceptBy(node.OurContact, c => c.ID.Value).ExceptBy(nodesToQuery, c => c.ID.Value).ToList();
            var nearestNodeDistance = nodesToQuery.OrderBy(n => n.ID.Value ^ key.Value).First().ID.Value;

            closerContacts.
                AddRangeDistinctBy(peersNodes.
                    Where(p => (p.ID.Value ^ key.Value) < nearestNodeDistance),
                    (a, b) => a.ID.Value == b.ID.Value);

            fartherContacts.
                AddRangeDistinctBy(peersNodes.
                    Where(p => (p.ID.Value ^ key.Value) >= nearestNodeDistance),
                    (a, b) => a.ID.Value == b.ID.Value);
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
            KBucket closest = Node.BucketList.Buckets.Where(b => b.Contacts.Count > 0).OrderBy(b => b.Key ^ key.Value).FirstOrDefault();
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
        /// For each contact, call the FindNodes and return all the nodes whose contacts responded
        /// within a "reasonable" period of time.
        /// </summary>
        protected List<Contact> RpcFindNodes(ID key, List<Contact> contacts)
        {
            List<Contact> nodes = new List<Contact>();
            contacts.ForEach(c => nodes.AddRange(c.Protocol.FindNode(Node.OurContact, key)));

            return nodes;
        }
    }
}
