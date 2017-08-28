using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;

namespace UnitTests
{
	[TestClass]
	public class IDTests
	{
		/// <summary>
		/// Test bucket indexing by testing all patterns:
		/// 1xxxxx   (idx 159)
		/// 01xxxx   (idx 158)
		/// 001xxx   (idx 157)
		/// etc. that we get the correct index back.
		/// </summary>
		[TestMethod]
		public void BuckIndexTest()
		{
			int byteIdx = 0;
			byte bitIdx = 0x80;

			for (int i = 0; i < Constants.ID_LENGTH_BITS; i++)
			{
				// bits 0 to i are 0, set bit i (counting from MSB), and set remaining bits (of the LSB's) to random values.
				byte[] idbytes = new byte[Constants.ID_LENGTH_BYTES];
				idbytes[byteIdx] = bitIdx;
				ID id = new ID(idbytes);
				id = id.RandomizeBeyond(i);
				int bucketIdx = id.GetBucketIndex();
				Assert.IsTrue(bucketIdx == (Constants.ID_LENGTH_BITS - (i+1)), "Bucket index does not match expected index.");


				bitIdx >>= 1;

				if (bitIdx == 0)
				{
					++byteIdx;
					bitIdx = 0x80;
				}
			}
		}
	}
}
