using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests
{
    [TestClass]
    public class DistributionTests
    {
        public const string CRLF = "\r\n";

        [TestMethod]
        public void DistributionZeroIDTest()
        {
            Node node = RouterTests.CreateNode(ID.ZeroID());
            TestDistribution(node, "distMin.txt");
        }

        [TestMethod]
        public void DistributionMidIDTest()
        {
            Node node = RouterTests.CreateNode(ID.MidID());
            TestDistribution(node, "distMid.txt");
        }

        [TestMethod]
        public void DistributionMaxIDTest()
        {
            Node node = RouterTests.CreateNode(ID.MaxID());
            TestDistribution(node, "distMax.txt");
        }

        private static void TestDistribution(Node node, string fn)
        {
            (Constants.ID_LENGTH_BITS * Constants.K).ForEach(() => node.SimpleRegistration(new Contact() { NodeID = ID.RandomID() }));
            var bucketCounts = node.BucketList.GetBucketContactCounts();
            Assert.IsTrue(bucketCounts.Select(b => b.idx).Average().ApproximatelyEquals(110, 5), "Bad distribution");
            Assert.IsTrue(bucketCounts.Select(b => b.count).Sum() > 3000, "Expected at least 3000 nodes to contacts into buckets.");
            Assert.IsTrue(bucketCounts.Select(b => (double)b.count).StdDev().ApproximatelyEquals(3, 1), "Bad distribution");
            Write(fn, bucketCounts);
        }

        private static void Write(string fn, List<(int idx, int count)> bucketCounts)
        {
            File.Delete(fn);
            StringBuilder sb = new StringBuilder();
            bucketCounts.ForEach(bc => sb.Append(bc.idx + "," + bc.count + CRLF));
            File.WriteAllText(fn, sb.ToString());
        }
    }
}
