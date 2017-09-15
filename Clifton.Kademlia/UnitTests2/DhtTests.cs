using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests2
{
    [TestClass]
    public class DhtTests
    {
        [TestMethod]
        public void LocalStoreFoundValueTest()
        {
            VirtualProtocol vp = new VirtualProtocol();
            Dht dht = new Dht(ID.RandomID, vp, new VirtualStorage());
            vp.Node = dht.Router.Node;
            ID key = ID.RandomID;
            string val = "Test";
            dht.Store(key, val);
            string retval = dht.FindValue(key).val;
            Assert.IsTrue(retval == val, "Expected to get back what we stored");
        }

        [TestMethod]
        public void ValueStoredInCloserNodeTest()
        {
            VirtualProtocol vp1 = new VirtualProtocol();
            VirtualProtocol vp2 = new VirtualProtocol();
            VirtualStorage store1 = new VirtualStorage();
            VirtualStorage store2 = new VirtualStorage();

            // Ensures that all nodes are closer, because ID.Max ^ n < ID.Max when n > 0.
            Dht dht = new Dht(ID.Max, vp1, store1);
            vp1.Node = dht.Router.Node;

            ID contactID = ID.Mid;      // a closer contact.
            Contact otherContact = new Contact(vp2, contactID);
            Node otherNode = new Node(otherContact, store2);
            vp2.Node = otherNode;
            
            // Add this other contact to our peer list.
            dht.Router.Node.BucketList.AddContact(otherContact);

            // We want an integer distance, not an XOR distance.
            ID key = ID.Zero;

            // Set the value in the other node, to be discovered by the lookup process.
            string val = "Test";
            otherNode.Cache(key, val);

            Assert.IsFalse(store1.Contains(key), "Expected our peer to NOT have cached the key-value.");

            // Try and find the value, given our Dht knows about the other contact.
            string retval = dht.FindValue(key).val;

            Assert.IsTrue(retval == val, "Expected to get back what we stored");
            Assert.IsTrue(store1.Contains(key), "Expected our peer to have cached the key-value.");
        }

        [TestMethod]
        public void ValueStoredInFartherNodeTest()
        {
            VirtualProtocol vp1 = new VirtualProtocol();
            VirtualProtocol vp2 = new VirtualProtocol();
            VirtualStorage store1 = new VirtualStorage();
            VirtualStorage store2 = new VirtualStorage();

            // Ensures that all nodes are closer, because ID.Max ^ n < ID.Max when n > 0.
            Dht dht = new Dht(ID.Zero, vp1, store1);
            vp1.Node = dht.Router.Node;

            ID contactID = ID.Max;      // a closer contact.
            Contact otherContact = new Contact(vp2, contactID);
            Node otherNode = new Node(otherContact, store2);
            vp2.Node = otherNode;

            // Add this other contact to our peer list.
            dht.Router.Node.BucketList.AddContact(otherContact);

            // We want an integer distance, not an XOR distance.
            ID key = ID.One;

            // Set the value in the other node, to be discovered by the lookup process.
            string val = "Test";
            otherNode.Cache(key, val);

            Assert.IsFalse(store1.Contains(key), "Expected our peer to NOT have cached the key-value.");

            // Try and find the value, given our Dht knows about the other contact.
            string retval = dht.FindValue(key).val;

            Assert.IsTrue(retval == val, "Expected to get back what we stored");
            Assert.IsTrue(store1.Contains(key), "Expected our peer to have cached the key-value.");
        }

        [TestMethod]
        public void ValueStoredGetsPropagatedTest()
        {
            VirtualProtocol vp1 = new VirtualProtocol();
            VirtualProtocol vp2 = new VirtualProtocol();
            VirtualStorage store1 = new VirtualStorage();
            VirtualStorage store2 = new VirtualStorage();

            // Ensures that all nodes are closer, because ID.Max ^ n < ID.Max when n > 0.
            Dht dht = new Dht(ID.Max, vp1, store1);
            vp1.Node = dht.Router.Node;

            ID contactID = ID.Mid;      // a closer contact.
            Contact otherContact = new Contact(vp2, contactID);
            Node otherNode = new Node(otherContact, store2);
            vp2.Node = otherNode;

            // Add this other contact to our peer list.
            dht.Router.Node.BucketList.AddContact(otherContact);

            // We want an integer distance, not an XOR distance.
            ID key = ID.Zero;
            string val = "Test";

            Assert.IsFalse(store1.Contains(key), "Obviously we don't have the key-value yet.");
            Assert.IsFalse(store2.Contains(key), "And equally obvious, the other peer doesn't have the key-value yet either.");

            dht.Store(key, val);

            Assert.IsTrue(store1.Contains(key), "Expected our peer to have stored the key-value.");
            Assert.IsTrue(store2.Contains(key), "Expected the other peer to have stored the key-value.");
        }

        [TestMethod]
        public void BootstrapWithinBootstrappingBucketTest()
        {
            // We need 22 virtual protocols.  One for the bootstrap peer,
            // 10 for the nodes the bootstrap peer knows about, and 10 for the nodes
            // one of those nodes knows about, and one for us to rule them all.
            VirtualProtocol[] vp = new VirtualProtocol[22];
            22.ForEach((i) => vp[i] = new VirtualProtocol());

            // Us
            Dht dhtUs = new Dht(ID.RandomID, vp[0], null);
            vp[0].Node = dhtUs.Router.Node;

            // Our bootstrap peer
            Dht dhtBootstrap = new Dht(ID.RandomID, vp[1], null);
            vp[1].Node = dhtBootstrap.Router.Node;
            Node n = null;

            // Our boostrapper knows 10 contacts
            10.ForEach((i) =>
            {
                Contact c = new Contact(vp[i + 2], ID.RandomID);
                n = new Node(c, null);
                vp[i + 2].Node = n;
                dhtBootstrap.Router.Node.BucketList.AddContact(c);
            });

            // One of those nodes, in this case the last one we added to our bootstrapper
            // for convenience, knows about 10 other contacts.
            10.ForEach((i) =>
            {
                Contact c = new Contact(vp[i + 12], ID.RandomID);
                Node n2 = new Node(c, null);
                vp[i + 12].Node = n;
                n.BucketList.AddContact(c);     // Note we're adding these contacts to the 10th node.
            });

            dhtUs.Bootstrap(dhtBootstrap.Router.Node.OurContact);

            Assert.IsTrue(dhtUs.Router.Node.BucketList.Buckets.Sum(c => c.Contacts.Count) == 11, "Expected our peer to get 11 contacts.");
        }

        [TestMethod]
        public void BootstrapOutsideBootstrappingBucketTest()
        {
            // We need 32 virtual protocols.  One for the bootstrap peer,
            // 20 for the nodes the bootstrap peer knows about, 10 for the nodes
            // one of those nodes knows about, and one for us to rule them all.
            VirtualProtocol[] vp = new VirtualProtocol[32];
            32.ForEach((i) => vp[i] = new VirtualProtocol());

            // Us, ID doesn't matter.
            Dht dhtUs = new Dht(ID.RandomID, vp[0], null);
            vp[0].Node = dhtUs.Router.Node;

            // Our bootstrap peer
            // All ID's are < 2^159
            Dht dhtBootstrap = new Dht(ID.Zero.RandomizeBeyond(Constants.ID_LENGTH_BITS - 1), vp[1], null);
            vp[1].Node = dhtBootstrap.Router.Node;
            Node n = null;

            // Our boostrapper knows 20 contacts
            20.ForEach((i) =>
            {
                ID id;

                // All ID's are < 2^159 except the last one, which is >= 2^159
                // which will force a bucket split for _us_
                if (i < 19)
                {
                    id = ID.Zero.RandomizeBeyond(Constants.ID_LENGTH_BITS - 1);
                }
                else
                {
                    id = ID.Max;
                }

                Contact c = new Contact(vp[i + 2], id);
                n = new Node(c, null);
                vp[i + 2].Node = n;
                dhtBootstrap.Router.Node.BucketList.AddContact(c);
            });

            // One of those nodes, in this case specifically the last one we added to our bootstrapper
            // so that it isn't in the bucket of our bootstrapper, we add 10 contacts.  The ID's of
            // those contacts don't matter.
            10.ForEach((i) =>
            {
                Contact c = new Contact(vp[i + 22], ID.RandomID);
                Node n2 = new Node(c, null);
                vp[i + 22].Node = n;
                n.BucketList.AddContact(c);     // Note we're adding these contacts to the 10th node.
            });

            dhtUs.Bootstrap(dhtBootstrap.Router.Node.OurContact);

            Assert.IsTrue(dhtUs.Router.Node.BucketList.Buckets.Sum(c => c.Contacts.Count) == 31, "Expected our peer to have 31 contacts.");
        }
    }
}
