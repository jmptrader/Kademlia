using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

// Some of this code is borrowed from here: https://github.com/zencoders/sambatyon/blob/master/Kademlia/Kademlia/ID.cs
// Credits to Dario Mazza, Sebastiano Merlino
// A lot of it has been refactored to use extension methods and Linq.

namespace Clifton.Kademlia
{
	public class ID : IComparable
	{
        // Used for unit testing:
        public byte[] ByteID { get { return id; } }

        private byte[] id;
		private static Random rnd = new Random();

		/// <summary>
		/// Make a new ID from a byte array.
		/// </summary>
		/// <param name="data">An array of exactly 20 bytes.</param>
		public ID(byte[] data)
		{
			IDInit(data);
		}

        private void IDInit(byte[] data)
		{
			Validate.IsTrue(data.Length == Constants.ID_LENGTH_BYTES, "ID must be " + Constants.ID_LENGTH_BYTES + " bytes in length.");
			id = new byte[Constants.ID_LENGTH_BYTES];
			data.CopyTo(id, 0);
		}

        public int GetBucketIndex()
        {
            return (Constants.ID_LENGTH_BITS - id.Bits().TakeWhile(b => !b).Count() - 1).Max(0);
        }

        public int DifferingBit(ID other)
        {
            ID differingBits = this ^ other;
            int differAt = 8 * Constants.ID_LENGTH_BYTES - 1;

            // Subtract 8 for every zero byte from the right
            int i = Constants.ID_LENGTH_BYTES - 1;
            while (i >= 0 && differingBits.id[i] == 0)
            {
                differAt -= 8;
                i--;
            }

            // Subtract 1 for every zero bit from the right
            int j = 0;
            // 1 << j = pow(2, j)
            while (j < 8 && (differingBits.id[i] & (1 << j)) == 0)
            {
                j++;
                differAt--;
            }

            return differAt;
        }


        /// <summary>
        /// Sets the bit n, from the LSB.
        /// </summary>
        public void SetBit(int n)
        {
            int m = (Constants.ID_LENGTH_BITS - 1) - n;
            id[m / 8] |= (byte)(1 << (n % 8));
        }

        /// <summary>
        /// Method used to get the hash code according to the algorithm: 
        /// http://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c/425184#425184
        /// </summary>
        /// <returns>integer representing the hashcode</returns>
        public override int GetHashCode()
		{
			int hash = 0;

			for (int i = 0; i < Constants.ID_LENGTH_BYTES; i++)
			{
				unchecked
				{
					hash *= 31;
				}

				hash ^= id[i];
			}
			return hash;
		}

		public override bool Equals(object obj)
		{
			Validate.IsTrue(obj is ID, "Cannot compare non-ID objects to an ID");

			return this == (ID)obj;
		}

        /// <summary>
        /// This method assumes that all bits from [0, bit) are 0.
        /// </summary>
		public ID RandomizeBeyond(int bit)
		{
			byte[] randomized = new byte[Constants.ID_LENGTH_BYTES];
			id.CopyTo(randomized, 0);
            ID newid = new ID(randomized);

			for (int i = 0;  i < bit; i++)
			{
				if (rnd.NextDouble() < 0.5)
				{
                    newid.SetBit(i);
				}
			}

			return newid;
		}

		/// <summary>
		/// Compare one ID with another.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>-1 if this ID < test ID, 0 if equal, 1 if this ID > test ID.</test></returns>
		public int CompareTo(object obj)
		{
			Validate.IsTrue(obj is ID, "Cannot compare non-ID objects to an ID");
			ID test = (ID)obj;

			return this == test ? 0 : this < test ? -1 : 1;
		}

		public override string ToString()
		{
			return BitConverter.ToString(id);
		}

		/// <summary>
		/// Returns a SHA1 of the string.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static ID FromString(string s)
		{
			HashAlgorithm hasher = new SHA1CryptoServiceProvider(); // Keeping this around results in exceptions

			return new ID(hasher.ComputeHash(Encoding.UTF8.GetBytes(s)));
		}

		/// <summary>
		/// Produce a random ID.
		/// </summary>
		/// <returns>random ID generated</returns>
		public static ID RandomID()
		{
			byte[] data = new byte[Constants.ID_LENGTH_BYTES];
            ID id = new ID(data);
            // Uniform random bucket index.
            int idx = rnd.Next(Constants.ID_LENGTH_BITS);
            // 0 <= idx <= 159
            // Remaining bits are randomized to get unique ID.
            id.SetBit(idx);
            id = id.RandomizeBeyond(idx);
            Validate.IsTrue(id.GetBucketIndex() == idx, "Error with RandomID.");

			return id;
		}

        public static ID ZeroID()
		{
			byte[] data = new byte[Constants.ID_LENGTH_BYTES];

			return new ID(data);
		}

		public static ID OneID()
		{
			byte[] data = new byte[Constants.ID_LENGTH_BYTES];
			data[Constants.ID_LENGTH_BYTES - 1] = 1;

			return new ID(data);
		}

		public static ID MaxID()
		{
			byte[] data = new byte[Constants.ID_LENGTH_BYTES];
			Constants.ID_LENGTH_BYTES.ForEach(n => data[n] = 0xFF);

			return new ID(data);
		}

		/// <summary>
		/// XOR operator.
		/// This is our distance metric in the DHT.
		/// </summary>
		/// <param name="a">The first ID to make xor</param>
		/// <param name="b">The second ID to make xor</param>
		/// <returns></returns>
		public static ID operator ^(ID a, ID b)
		{
			byte[] xoredData = new byte[Constants.ID_LENGTH_BYTES];
			Constants.ID_LENGTH_BYTES.ForEach(n => xoredData[n] = (byte)(a.id[n] ^ b.id[n]));

			return new ID(xoredData);
		}

		/// <summary>
		/// We need to compare these when measuring distance
		/// </summary>
		/// <param name="a">First ID to compare</param>
		/// <param name="b">Second ID to compare</param>
		/// <returns>true if a is less than b; false otherwise</returns>
		public static bool operator <(ID a, ID b)
		{
			return Constants.ID_LENGTH_BYTES.Range().SkipWhile(n => a.id[n] == b.id[n]).IsNext(n => a.id[n] < b.id[n]);
		}

		/// <summary>
		/// We need to compare these when measuring distance
		/// </summary>
		/// <param name="a">First ID to compare</param>
		/// <param name="b">Second ID to compare</param>
		/// <returns>true if a is greater than b; false otherwise</returns>
		public static bool operator >(ID a, ID b)
		{
			return Constants.ID_LENGTH_BYTES.Range().SkipWhile(n => a.id[n] == b.id[n]).IsNext(n => a.id[n] > b.id[n]);
		}

		/// <summary>
		/// We need to compare these when measuring distance
		/// </summary>
		/// <param name="a">First ID to compare</param>
		/// <param name="b">Second ID to compare</param>
		/// <returns>true if a is equals to b; false otherwise</returns>
		public static bool operator ==(ID a, ID b)
		{
			Validate.IsFalse(ReferenceEquals(a, null), "ID a cannot be null.");
			Validate.IsFalse(ReferenceEquals(b, null), "ID b cannot be null.");

			return Constants.ID_LENGTH_BYTES.Range().All(n => a.id[n] == b.id[n]);
		}

		/// <summary>
		/// We need to compare these when measuring distance
		/// </summary>
		/// <param name="a">First ID to compare</param>
		/// <param name="b">Second ID to compare</param>
		/// <returns>true if a is different from b; false otherwise</returns>
		public static bool operator !=(ID a, ID b)
		{
			return !(a == b); // Already have that
		}

		/// <summary>
		/// Shift all bits left.
		/// </summary>
		public static ID operator <<(ID id, int count)
		{
			byte[] result = new byte[Constants.ID_LENGTH_BYTES];
			id.id.CopyTo(result, 0);
			byte carry = 0;

            while (count-- > 0)
            {
                for (int i = Constants.ID_LENGTH_BYTES - 1; i >= 0; i--)
                {
                    byte nextCarry = (byte)((result[i] & 0x80) >> 7);
                    result[i] = (byte)((result[i] << 1) | carry);
                    carry = nextCarry;
                }
            }

			return new ID(result);
		}

        /// <summary>
        /// Shift all bits right.
        /// </summary>
        public static ID operator >>(ID id, int count)
        {
            byte[] result = new byte[Constants.ID_LENGTH_BYTES];
            id.id.CopyTo(result, 0);
            byte carry = 0;

            while (count-- > 0)
            {
                for (int i = 0; i< Constants.ID_LENGTH_BYTES; i++)
                {
                    byte nextCarry = (byte)((result[i] & 0x01) << 7);
                    result[i] = (byte)((result[i] >> 1) | carry);
                    carry = nextCarry;
                }
            }

            return new ID(result);
        }
    }
}
