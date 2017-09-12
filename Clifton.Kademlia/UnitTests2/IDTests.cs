using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests2
{
    [TestClass]
    public class IDTests
    {
        [TestMethod]
        public void LittleEndianTest()
        {
            byte[] test = new byte[20];
            test[0] = 1;
            Assert.IsTrue(new ID(test).Value == new BigInteger(1), "Expected value to be 1.");
        }

        [TestMethod]
        public void PositiveValueTest()
        {
            byte[] test = new byte[20];
            test[19] = 0x80;
            Assert.IsTrue(new ID(test).Value == BigInteger.Pow(new BigInteger(2), 159), "Expected value to be 1.");
        }

        [TestMethod, ExpectedException(typeof(IDLengthException))]
        public void BadIDTest()
        {
            byte[] test = new byte[21];
            new ID(test);
        }
    }
}
