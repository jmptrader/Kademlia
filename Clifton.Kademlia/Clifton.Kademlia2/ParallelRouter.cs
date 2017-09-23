// #define TRY_CLOSEST_BUCKET

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Clifton.Kademlia
{
    public class ContactQueueItem
    {
        public ID Key { get; set; }
        public Contact Contact { get; set; }
        public Func<ID, List<Contact>, (List<Contact> contacts, Contact foundBy, string val)> RpcCall { get; set; }
        public List<Contact> CloserContacts { get; set; }
        public List<Contact> FartherContacts { get; set; }
    }

    public class ParallelRouter : BaseRouter
    {
        // ======================
        // For parallel querying:
        protected ConcurrentQueue<ContactQueueItem> contactQueue;
        protected Semaphore semaphore;
        protected List<Thread> threads;

        protected bool found;
        protected List<Contact> foundContacts;
        protected Contact foundBy;
        protected string foundValue;
        // ======================

        public ParallelRouter(Node node)
        {
            this.node = node;
            contactQueue = new ConcurrentQueue<ContactQueueItem>();
            semaphore = new Semaphore(0, Int32.MaxValue);
            InitializeThreadPool();
        }

        public override (bool found, List<Contact> contacts, Contact foundBy, string val) Lookup(
            ID key,
            Func<ID, List<Contact>, (List<Contact> contacts, Contact foundBy, string val)> rpcCall,
            bool giveMeAll = false)
        {
            bool haveWork = true;
            List<Contact> ret = new List<Contact>();
            List<Contact> contactedNodes = new List<Contact>();
            List<Contact> closerContacts = new List<Contact>();
            List<Contact> fartherContacts = new List<Contact>();
            (bool found, List<Contact> contacts, Contact foundBy, string val) foundReturn = (false, null, null, String.Empty);

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
            closerContacts.AddRange(nodesToQuery.Where(n => (n.ID ^ key) < (node.OurContact.ID ^ key)));
            fartherContacts.AddRange(nodesToQuery.Where(n => (n.ID ^ key) >= (node.OurContact.ID ^ key)));

            // The remaining contacts not tested yet can be put here.
            fartherContacts.AddRange(allNodes.Skip(Constants.ALPHA).Take(Constants.K - Constants.ALPHA));
#endif

            // We're about to contact these nodes.
            contactedNodes.AddRangeDistinctBy(nodesToQuery, (a, b) => a.ID == b.ID);

            // Spec: The initiator then sends parallel, asynchronous FIND_NODE RPCS to the a nodes it has chosen, 
            // a is a system-wide concurrency parameter, such as 3.

            nodesToQuery.ForEach(n => AddToQueue(key, n, rpcCall, closerContacts, fartherContacts));

            // Add any new closer contacts to the list we're going to return.
            ret.AddRangeDistinctBy(closerContacts, (a, b) => a.ID == b.ID);

            // Spec: The lookup terminates when the initiator has queried and gotten responses from the k closest nodes it has seen.
            while (ret.Count < Constants.K && haveWork)
            {
                if (ParallelFound(ref foundReturn))
                {
                    DequeueRemainingWork();
                    return foundReturn;
                }

                List<Contact> closerUncontactedNodes = closerContacts.Except(contactedNodes).ToList();
                List<Contact> fartherUncontactedNodes = fartherContacts.Except(contactedNodes).ToList();
                bool haveCloser = closerUncontactedNodes.Count > 0;
                bool haveFarther = fartherUncontactedNodes.Count > 0;

                haveWork = haveCloser || haveFarther;

                // Spec:  Of the k nodes the initiator has heard of closest to the target...
                if (haveCloser)
                {
                    // We're about to contact these nodes.
                    var alphaNodes = closerUncontactedNodes.Take(Constants.ALPHA);
                    contactedNodes.AddRangeDistinctBy(alphaNodes, (a, b) => a.ID == b.ID);
                    alphaNodes.ForEach(n => AddToQueue(key, n, rpcCall, closerContacts, fartherContacts));
                }
                else if (haveFarther)
                {
                    // We're about to contact these nodes.
                    var alphaNodes = fartherUncontactedNodes.Take(Constants.ALPHA);
                    contactedNodes.AddRangeDistinctBy(alphaNodes, (a, b) => a.ID == b.ID);
                    alphaNodes.ForEach(n => AddToQueue(key, n, rpcCall, closerContacts, fartherContacts));
                }
            }

#if DEBUG       // For unit testing.
            CloserContacts = closerContacts;
            FartherContacts = fartherContacts;
#endif

            DequeueRemainingWork();

            // Spec (sort of): Return max(k) closer nodes, sorted by distance.
            // For unit testing, giveMeAll can be true so that we can match against our alternate way of getting closer contacts.
            return (false, (giveMeAll ? ret : ret.Take(Constants.K).OrderBy(c => c.ID ^ key).ToList()), null, null);
        }

        protected void InitializeThreadPool()
        {
            threads = new List<Thread>();
            Constants.MAX_THREADS.ForEach(() =>
            {
                Thread thread = new Thread(new ThreadStart(RpcCaller));
                thread.IsBackground = true;
                thread.Start();
            });
        }

        protected void RpcCaller()
        {
            while (true)
            {
                semaphore.WaitOne();
                ContactQueueItem item;

                if (contactQueue.TryDequeue(out item))
                {
                    string val;
                    Contact foundBy;
                    List<Contact> contacts = new List<Contact>();
                    contacts.Add(item.Contact);

                    if (ParallelGetCloserNodes(
                        item.Key,
                        contacts,
                        item.RpcCall,
                        item.CloserContacts,
                        item.FartherContacts,
                        out val,
                        out foundBy))
                    {
                        // Possible multiple "found"
                        lock (this)
                        {
                            found = true;
                            this.foundBy = foundBy;
                            foundValue = val;
                            foundContacts = item.CloserContacts;
                        }
                    }
                }
            }
        }

        protected void AddToQueue(
            ID key,
            Contact contact,
            Func<ID, List<Contact>, (List<Contact> contacts, Contact foundBy, string val)> rpcCall,
            List<Contact> closerContacts,
            List<Contact> fartherContacts
            )
        {
            contactQueue.Enqueue(new ContactQueueItem()
            {
                Key = key,
                Contact = contact,
                RpcCall = rpcCall,
                CloserContacts = closerContacts,
                FartherContacts = fartherContacts
            });

            semaphore.Release();
        }

        protected void DequeueRemainingWork()
        {
            while (contactQueue.TryDequeue(out _)) { };
        }

        protected bool ParallelFound(ref (bool found, List<Contact> contacts, Contact foundBy, string val) foundRet)
        {
            if (found)
            {
                // Prevent found contacts from changing as we clone the found contacts list.
                lock (foundContacts)
                {
                    foundRet = (true, foundContacts.ToList(), foundBy, foundValue);
                }
            }

            return found;
        }

        protected bool ParallelGetCloserNodes(
            ID key,
            List<Contact> nodeToQuery,
            Func<ID, List<Contact>, (List<Contact> contacts, Contact foundBy, string val)> rpcCall,
            List<Contact> closerContacts,
            List<Contact> fartherContacts,
            out string val,
            out Contact foundBy)
        {
            // As in, peer's nodes:
            // Exclude ourselves and the peers we're contacting to a get unique list of new peers.
            // Compare by ID's as Contact is different instance except with a virtual network.
            var (contacts, cFoundBy, foundVal) = rpcCall(key, nodeToQuery);
            val = foundVal;
            foundBy = cFoundBy;
            List<Contact> peersNodes = contacts.ExceptBy(node.OurContact, c => c.ID).ExceptBy(nodeToQuery, c => c.ID).ToList();

            // Null continuation is a special case primarily for unit testing when we have no nodes in any buckets.
            var nearestNodeDistance = nodeToQuery[0].ID ^ key;

            lock (closerContacts)
            {
                closerContacts.
                    AddRangeDistinctBy(peersNodes.
                        Where(p => (p.ID ^ key) < nearestNodeDistance),
                        (a, b) => a.ID == b.ID);
            }

            lock (fartherContacts)
            {
                fartherContacts.
                    AddRangeDistinctBy(peersNodes.
                        Where(p => (p.ID ^ key) >= nearestNodeDistance),
                        (a, b) => a.ID == b.ID);
            }

            return val != null;
        }

        public override (List<Contact> contacts, Contact foundBy, string val) RpcFindNodes(ID key, List<Contact> contacts)
        {
            List<Contact> nodes = new List<Contact>();
            nodes.AddRange(contacts[0].Protocol.FindNode(node.OurContact, key));

            return (nodes, null, null);
        }

        /// <summary>
        /// For each contact, call the FindNode and return all the nodes whose contacts responded
        /// within a "reasonable" period of time, unless a value is returned, at which point we stop.
        /// </summary>
        public override (List<Contact> contacts, Contact foundBy, string val) RpcFindValue(ID key, List<Contact> contacts)
        {
            List<Contact> nodes = new List<Contact>();
            string retval = null;
            Contact foundBy = null;

            (var otherContacts, var val) = contacts[0].Protocol.FindValue(node.OurContact, key);

            if (otherContacts != null)
            {
                nodes.AddRange(otherContacts);
            }
            else
            {
                Validate.IsTrue<ValueCannotBeNullException>(val != null, "Null values are not supported nor expected.");
                nodes.Add(contacts[0]);           // The node we just contacted found the value.
                foundBy = contacts[0];
                retval = val;
            }

            return (nodes, foundBy, retval);
        }
    }
}