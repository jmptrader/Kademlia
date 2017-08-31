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
        public void DistributionMidpointTest()
        {
            Node node = RouterTests.CreateNode(ID.ZeroID());
            // node.OurContact.NodeID.SetBit(80);
            (Constants.ID_LENGTH_BITS * Constants.K).ForEach(() => node.SimpleRegistration(new Contact() { NodeID = ID.RandomID() }));
            var bucketCounts = node.BucketList.GetBucketContactCounts();
            Assert.IsTrue(bucketCounts.Select(b => b.idx).Average().ApproximatelyEquals(80, 2), "Bad distribution");
            Assert.IsTrue(bucketCounts.Select(b=>b.count).Sum() > 2800, "Expected at least 2800 nodes to register into buckets.");
            Assert.IsTrue(bucketCounts.Select(b => (double)b.count).StdDev().ApproximatelyEquals(3, 1), "Bad distribution");
            Write("countsMidpoint.txt", bucketCounts);
        }

        public static void Write(string fn, List<(int idx, int count)> bucketCounts)
        {
            File.Delete(fn);
            StringBuilder sb = new StringBuilder();
            bucketCounts.ForEach(bc => sb.Append(bc.idx + "," + bc.count + CRLF));
            File.WriteAllText(fn, sb.ToString());
        }
    }
}
