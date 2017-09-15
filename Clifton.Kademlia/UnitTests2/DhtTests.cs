using System;
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

            // Set the value in the other node, to be discovered by the lookup process.
            string val = "Test";

            Assert.IsFalse(store1.Contains(key), "Obviously we don't have the key-value yet.");
            Assert.IsFalse(store2.Contains(key), "And equally obvious, our peer doesn't have the key-value yet either.");

            dht.Store(key, val);

            Assert.IsTrue(store1.Contains(key), "Expected our peer to have cached the key-value.");
            Assert.IsTrue(store2.Contains(key), "Expected our peer to have cached the key-value.");
        }
    }
}
