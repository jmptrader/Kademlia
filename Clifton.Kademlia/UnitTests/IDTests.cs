using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests
{
    [TestClass]
    public class IDTests
    {
        [TestMethod]
        public void CompareToTests()
        {
            Assert.IsTrue(ID.OneID().CompareTo(ID.MaxID()) == -1, "Expected OneID < MaxID");
            Assert.IsTrue(ID.OneID().CompareTo(ID.OneID()) == 0, "Expected OneID == OneID");
            Assert.IsTrue(ID.MaxID().CompareTo(ID.OneID()) == 1, "Expected MaxID > OneID");
        }

        [TestMethod]
        public void ComparisonTests()
        {
            Assert.IsTrue(ID.ZeroID() < ID.MaxID(), "Expected ZeroID < MaxID");
            Assert.IsTrue(ID.OneID() < ID.MaxID(), "Expected OneID < MaxID");
            Assert.IsTrue(ID.ZeroID() < ID.OneID(), "Expected ZeroID < OneID");
            Assert.IsTrue(ID.OneID() == ID.OneID(), "Expected OneID == OneID");
            Assert.IsTrue(ID.MaxID() > ID.ZeroID(), "Expected MaxID > ZeroID");
            Assert.IsTrue(ID.MaxID() > ID.OneID(), "Expected MaxID > OneID");
            Assert.IsTrue(ID.OneID() > ID.ZeroID(), "Expected OneID > ZeroID");

            // edge cases:
            Assert.IsFalse(ID.ZeroID() < ID.ZeroID(), "Expected OneID == OneID");
            Assert.IsFalse(ID.ZeroID() > ID.ZeroID(), "Expected OneID == OneID");
            Assert.IsFalse(ID.MaxID() < ID.MaxID(), "Expected OneID == OneID");
            Assert.IsFalse(ID.MaxID() > ID.MaxID(), "Expected OneID == OneID");
        }

        [TestMethod]
        public void XorTest()
        {
            Assert.IsTrue((ID.MaxID() ^ ID.MaxID()) == ID.ZeroID(), "XOR failure.");
        }

        [TestMethod]
        public void ShiftLeftTest()
        {
            ID id = ID.ZeroID();
            id.SetBit(7);
            id = id << 1;
            Assert.IsTrue(id.Bytes[1] == 0x01, "Carry failed.");
            Assert.IsTrue(id.Bytes[0] == 0, "Shift left failed.");
        }

        [TestMethod]
        public void ShiftRightTest()
        {
            ID id = ID.ZeroID();
            id.SetBit(8);
            id = id >> 1;
            Assert.IsTrue(id.Bytes[1] == 0, "Shift right failed.");
            Assert.IsTrue(id.Bytes[0] == 0x80, "Carry failed.");
        }
    }
}
