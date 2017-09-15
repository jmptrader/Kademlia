using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests2
{
    [TestClass]
    public class LookupTests
    {
        [TestMethod]
        public void GetCloserNodesTest()
        {
            // Seed with different random values
            100.ForEach(seed =>
            {
                ID.rnd = new Random(seed);
                // Setup
                Router router = new Router(new Node(new Contact(null, ID.RandomID), null));

                List<Node> nodes = new List<Node>();
                100.ForEach(() => nodes.Add(new Node(new Contact(null, ID.RandomID), null)));

                // Fixup protocols:
                nodes.ForEach(n => n.OurContact.Protocol = new VirtualProtocol(n));

                // Our contacts:
                nodes.ForEach(n => router.Node.BucketList.AddContact(n.OurContact));

                // Each peer needs to know about the other peers except of course itself.
                nodes.ForEach(n => nodes.Where(nOther => nOther != n).ForEach(nOther => n.BucketList.AddContact(nOther.OurContact)));

                ID key = ID.RandomID;            // Pick an ID
                // TODO: Pick a random bucket, or bucket where the key is in range, otherwise we're defeating the purpose of the algorithm.
                // List<Contact> nodesToQuery = router.Node.BucketList.GetCloseContacts(key, router.Node.OurContact.ID).Take(Constants.ALPHA).ToList();

                List<Contact> nodesToQuery = router.Node.BucketList.GetKBucket(key).Contacts.Take(Constants.ALPHA).ToList();
                // or
                // router.FindClosestNonEmptyKBucket(key).Contacts.Take(Constants.ALPHA).ToList();

                List<Contact> closerContacts = new List<Contact>();
                List<Contact> fartherContacts = new List<Contact>();

                // Setup done.

                router.GetCloserNodes(key, nodesToQuery, closerContacts, fartherContacts);

                // Test whether the results are correct:  

                List<Contact> closerContactsAltComputation = new List<Contact>();
                List<Contact> fartherContactsAltComputation = new List<Contact>();
                Contact theNearestContactedNode = nodesToQuery.OrderBy(n => n.ID.Value ^ key.Value).First();
                var distance = theNearestContactedNode.ID.Value;

                // For each node (ALPHA == K for testing) in our bucket (nodesToQuery) we're going to get k nodes closest to the key:
                foreach (Contact contact in nodesToQuery)
                {
                    // Find the node we're contacting:
                    Node contactNode = nodes.Single(n => n.OurContact == contact);
                    
                    // Close contacts except ourself and the nodes we're contacting.
                    var closeContactsOfContactedNode =
                        contactNode.
                            BucketList.
                            GetCloseContacts(key, router.Node.OurContact.ID).
                            ExceptBy(nodesToQuery, c => c.ID.Value);

                    foreach (Contact closeContactOfContactedNode in closeContactsOfContactedNode)
                    {
                        // Which of these contacts are closer?
                        if ((closeContactOfContactedNode.ID.Value ^ key.Value) < distance)
                        {
                            closerContactsAltComputation.AddDistinctBy(closeContactOfContactedNode, c => c.ID.Value);
                        }

                        // Which of these contacts are farther?
                        if ((closeContactOfContactedNode.ID.Value ^ key.Value) >= distance)
                        {
                            fartherContactsAltComputation.AddDistinctBy(closeContactOfContactedNode, c => c.ID.Value);
                        }
                    }
                }

                Assert.IsTrue(closerContacts.Count == closerContactsAltComputation.Count, "Closer computations do not match.");
                Assert.IsTrue(fartherContacts.Count == fartherContactsAltComputation.Count, "Farther computations do not match.");
            });
       }
    }
}
