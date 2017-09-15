using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests2
{
    [TestClass]
    public class LookupTests
    {
        protected Router router;
        protected List<Node> nodes;
        protected ID key;
        protected List<Contact> contactsToQuery;
        protected List<Contact> closerContacts;
        protected List<Contact> fartherContacts;
        protected List<Contact> closerContactsAltComputation;
        protected List<Contact> fartherContactsAltComputation;
        Contact theNearestContactedNode;
        BigInteger distance;


        [TestMethod]
        public void GetCloserNodesTest()
        {
            // Seed with different random values
            100.ForEach(seed =>
            {
                ID.rnd = new Random(seed);
                Setup();

                router.GetCloserNodes(key, contactsToQuery, closerContacts, fartherContacts);

                // Test whether the results are correct:  

                GetAltCloseAndFar(contactsToQuery, closerContactsAltComputation, fartherContactsAltComputation);

                Assert.IsTrue(closerContacts.Count == closerContactsAltComputation.Count, "Closer computations do not match.");
                Assert.IsTrue(fartherContacts.Count == fartherContactsAltComputation.Count, "Farther computations do not match.");
            });
        }

        [TestMethod]
        public void LookupTest()
        {
            // Seed with different random values
            100.ForEach(seed =>
            {
                ID.rnd = new Random(seed);
                Setup();

                List<Contact> closeContacts = router.Lookup(key, true);
                List<Contact> contactedNodes = new List<Contact>(closeContacts);

                // Is the above call returning the correct number of close contacts?
                // The unit test for this is sort of lame.  We should get at least as many contacts 
                // as when calling GetCloserNodes.  

                GetAltCloseAndFar(contactsToQuery, closerContactsAltComputation, fartherContactsAltComputation);

                Assert.IsTrue(closeContacts.Count >= closerContactsAltComputation.Count, "Expected at least as many contacts.");

                // Technically, we can't even test whether the contacts returned in GetCloserNodes exists
                // in router.Lookup because it may have found nodes even closer, and it only returns K nodes!
                // We can overcome this by eliminating the Take in the return of router.Lookup().

                closerContactsAltComputation.ForEach(c => Assert.IsTrue(closeContacts.Contains(c)));
            });
        }

        protected void Setup()
        {
            // Setup
            router = new Router(new Node(new Contact(null, ID.RandomID), null));

            nodes = new List<Node>();
            100.ForEach(() => nodes.Add(new Node(new Contact(null, ID.RandomID), null)));

            // Fixup protocols:
            nodes.ForEach(n => n.OurContact.Protocol = new VirtualProtocol(n));

            // Our contacts:
            nodes.ForEach(n => router.Node.BucketList.AddContact(n.OurContact));

            // Each peer needs to know about the other peers except of course itself.
            nodes.ForEach(n => nodes.Where(nOther => nOther != n).ForEach(nOther => n.BucketList.AddContact(nOther.OurContact)));

            key = ID.RandomID;            // Pick an ID
            // TODO: Pick a random bucket, or bucket where the key is in range, otherwise we're defeating the purpose of the algorithm.
            // DO NOT DO THIS:
            // List<Contact> nodesToQuery = router.Node.BucketList.GetCloseContacts(key, router.Node.OurContact.ID).Take(Constants.ALPHA).ToList();

            contactsToQuery = router.Node.BucketList.GetKBucket(key).Contacts.Take(Constants.ALPHA).ToList();
            // or:
            // contactsToQuery = router.FindClosestNonEmptyKBucket(key).Contacts.Take(Constants.ALPHA).ToList();

            closerContacts = new List<Contact>();
            fartherContacts = new List<Contact>();

            closerContactsAltComputation = new List<Contact>();
            fartherContactsAltComputation = new List<Contact>();
            theNearestContactedNode = contactsToQuery.OrderBy(n => n.ID.Value ^ key.Value).First();
            distance = theNearestContactedNode.ID.Value;
        }

        protected void GetAltCloseAndFar(List<Contact> contactsToQuery, List<Contact> closer, List<Contact> farther)
        {
            // For each node (ALPHA == K for testing) in our bucket (nodesToQuery) we're going to get k nodes closest to the key:
            foreach (Contact contact in contactsToQuery)
            {
                // Find the node we're contacting:
                Node contactNode = nodes.Single(n => n.OurContact == contact);

                // Close contacts except ourself and the nodes we're contacting.
                // Note that of all the contacts in the bucket list, many of the k returned
                // by the GetCloseContacts call are contacts we're querying, so they are being excluded.
                var closeContactsOfContactedNode =
                    contactNode.
                        BucketList.
                        GetCloseContacts(key, router.Node.OurContact.ID).
                        ExceptBy(contactsToQuery, c => c.ID.Value);

                foreach (Contact closeContactOfContactedNode in closeContactsOfContactedNode)
                {
                    // Which of these contacts are closer?
                    if ((closeContactOfContactedNode.ID.Value ^ key.Value) < distance)
                    {
                        closer.AddDistinctBy(closeContactOfContactedNode, c => c.ID.Value);
                    }

                    // Which of these contacts are farther?
                    if ((closeContactOfContactedNode.ID.Value ^ key.Value) >= distance)
                    {
                        farther.AddDistinctBy(closeContactOfContactedNode, c => c.ID.Value);
                    }
                }
            }
        }
    }
}
