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

            KBucket bucket = FindClosestNonEmptyKBucket(key);

            // Spec: The lookup initiator starts by picking a nodes from its closest non-empty k-bucket
            List<Contact> nodesToQuery = GetClosestNodes(key, bucket).Take(Constants.ALPHA).ToList();

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
        protected void GetCloserNodes(ID key, List<Contact> nodesToQuery, List<Contact> closerContacts, List<Contact> fartherContacts)
        {
            // As in, peer's nodes:
            List<Contact> peersNodes = RpcFindNodes(key, nodesToQuery).Where(c => c.ID.Value != node.OurContact.ID.Value).ToList();
            closerContacts.AddRangeDistinctBy(peersNodes.WhereAll(nodesToQuery, (a, b) => (a.ID.Value ^ key.Value) < b.ID.Value), (a, b) => a.ID.Value == b.ID.Value);
            fartherContacts.AddRangeDistinctBy(peersNodes.WhereAll(nodesToQuery, (a, b) => (a.ID.Value ^ key.Value) >= b.ID.Value), (a, b) => a.ID.Value == b.ID.Value);
        }

        /// <summary>
        /// Using the k-bucket's key (it's high value), find the closest 
        /// k-bucket the given key that isn't empty.
        /// </summary>
        protected virtual KBucket FindClosestNonEmptyKBucket(ID key)
        {
            KBucket closest = Node.BucketList.Buckets.Where(b => b.Contacts.Count > 0).OrderBy(b => b.Key ^ key.Value).FirstOrDefault();
            Validate.IsTrue<NoNonEmptyBucketsException>(closest != null, "No non-empty buckets exist.  You must first register a peer and add that peer to your bucketlist.");

            return closest;
        }

        /// <summary>
        /// Get sorted list of closest nodes to the given key.
        /// </summary>
        protected List<Contact> GetClosestNodes(ID key, KBucket bucket)
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
            contacts.ForEach(c => nodes.AddRange(c.Protocol.FindNode(key)));

            return nodes;
        }
    }
}
