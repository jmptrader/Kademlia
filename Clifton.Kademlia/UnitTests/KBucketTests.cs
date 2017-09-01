using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests
{
    [TestClass]
    public class KBucketTests
    {
        /// <summary>
        /// Test that contacts added to a kbucket are in least to most-recently seen order.
        /// </summary>
        [TestMethod]
        public void LeastSeenOrderingTest()
        {
            KBucket kbucket = new KBucket();
            ID id = ID.OneID();

            40.ForEach(() =>
            {
                kbucket.AddContact(new Contact() { NodeID = id }, contact => false);
                id <<= 1;               // id's should be different.
                Thread.Sleep(2);        // need to have some time go by so DateTime's are different.
            });

            Assert.IsTrue(kbucket.Contacts.Count == Constants.K, "Expected k contacts.");

            DateTime last = default(DateTime);
            kbucket.Contacts.ForEach(contact =>
            {
                Assert.IsTrue(last < contact.LastSeen, "Contacts are out of order with regards to last seen.");
                last = contact.LastSeen;
            });
        }

        [TestMethod]
        public void BasicHasInRangeTests()
        {
            KBucket b = new KBucket();
            Assert.IsTrue(b.HasInRange(ID.ZeroID()), "ID of 0 should be in range.");
            Assert.IsTrue(b.HasInRange(ID.MaxID()), "Max ID should be in range.");
        }

        /// <summary>
        /// Creates two contacts that share the maximum number of bits.
        /// </summary>
        [TestMethod]
        public void MaxDepthTest()
        {
            KBucket b = new KBucket();
            b.AddContact(new Contact() { NodeID = ID.ZeroID() });
            b.AddContact(new Contact() { NodeID = ID.OneID() });

            Assert.IsTrue(b.Depth() == Constants.ID_LENGTH_BITS - 1, "Depth expected to be maximum possible depth.");
        }

        /// <summary>
        /// Creates two contacts that do not share any bits.
        /// </summary>
        [TestMethod]
        public void MinDepthTest()
        {
            KBucket b = new KBucket();
            b.AddContact(new Contact() { NodeID = ID.ZeroID() });
            b.AddContact(new Contact() { NodeID = ID.MaxID() });

            Assert.IsTrue(b.Depth() == 0, "Expected 0 shared bits.");
        }

        /// <summary>
        /// Shares 1 bit.
        /// </summary>
        [TestMethod]
        public void Depth1Test()
        {
            KBucket b = new KBucket();
            b.AddContact(new Contact() { NodeID = ID.ZeroID() });
            ID id = ID.ZeroID();
            id.SetBit(Constants.ID_LENGTH_BITS - 2);
            b.AddContact(new Contact() { NodeID = id });

            Assert.IsTrue(b.Depth() == 1, "Expected 1 shared bits.");
        }

        /// <summary>
        /// Shares 2 bits.
        /// </summary>
        [TestMethod]
        public void Depth2Test()
        {
            KBucket b = new KBucket();
            b.AddContact(new Contact() { NodeID = ID.ZeroID() });
            ID id = ID.ZeroID();
            id.SetBit(Constants.ID_LENGTH_BITS - 3);
            b.AddContact(new Contact() { NodeID = id });

            Assert.IsTrue(b.Depth() == 2, "Expected 2 shared bits.");
        }
    }
}
