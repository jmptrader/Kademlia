using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

// Some of this code is borrowed from here: https://github.com/zencoders/sambatyon/blob/master/Kademlia/Kademlia/ID.cs
// Credits to Dario Mazza, Sebastiano Merlino
// A lot of it has been refactored to use extension methods and Linq.

namespace Clifton.Kademlia
{
	public class ID : IComparable
	{
        public BigInteger Value { get { return id; } }

        // Zero-pad msb's if ToByteArray length != Constants.LENGTH_BYTES
        // The array returned is in little-endian order (lsb at index 0)
        public byte[] Bytes
        {
            get
            {
                byte[] bytes = new byte[Constants.ID_LENGTH_BYTES];
                byte[] partial = id.ToByteArray().Take(Constants.ID_LENGTH_BYTES).ToArray();    // remove msb 0 at index 20.
                partial.CopyTo(bytes, 0);

                return bytes;
            }
        }

        private BigInteger id;
		private static Random rnd = new Random();

        /// <summary>
        /// Creates a new ID from the *** little endian *** byte[].
        /// </summary>
		public ID(byte[] data)
		{
			IDInit(data);
		}

        public ID(BigInteger bi)
        {
            id = bi;
        }

        private void IDInit(byte[] data)
        {
            Validate.IsTrue(data.Length == Constants.ID_LENGTH_BYTES, "ID must be " + Constants.ID_LENGTH_BYTES + " bytes in length.");
            id = new BigInteger(data.Concat0());       // concat a 0 to force unsigned values.
		}

        /// <summary>
        /// Clears the bit n, from the LSB.
        /// </summary>
        public void ClearBit(int n)
        {
            byte[] bytes = Bytes;
            bytes[n / 8] &= (byte)((1 << (n % 8)) ^ 0xFF);
            id = new BigInteger(bytes.Concat0());
        }

        /// <summary>
        /// Sets the bit n, from the LSB.
        /// </summary>
        public void SetBit(int n)
        {
            byte[] bytes = Bytes;
            bytes[n / 8] |= (byte)(1 << (n % 8));
            id = new BigInteger(bytes.Concat0());
        }

        /// <summary>
        /// Method used to get the hash code according to the algorithm: 
        /// http://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c/425184#425184
        /// </summary>
        /// <returns>integer representing the hashcode</returns>
        public override int GetHashCode()
		{
            return id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			Validate.IsTrue(obj is ID, "Cannot compare non-ID objects to an ID");

			return this == (ID)obj;
		}

		public ID RandomizeBeyond(int bit)
		{
			byte[] randomized = Bytes;

            ID newid = new ID(randomized);

            // TODO: Optimize
            for (int i = bit + 1; i < Constants.ID_LENGTH_BITS; i++)
            {
                newid.ClearBit(i);
            }

            // TODO: Optimize
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
			return id.ToString();
		}

		/// <summary>
		/// Returns a SHA1 of the string.
		/// </summary>
		public static ID FromString(string s)
		{
            // Keeping this around results in exceptions
            using (HashAlgorithm hasher = new SHA1CryptoServiceProvider())
            {
                return new ID(hasher.ComputeHash(Encoding.UTF8.GetBytes(s)));
            }
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
			data[0] = 1;

			return new ID(data);
		}

        public static ID MidID()
        {
            byte[] data = new byte[Constants.ID_LENGTH_BYTES];
            data[Constants.ID_LENGTH_BYTES - 1] = 0x80;

            return new ID(data);
        }

        public static ID MaxID()
		{
            return new ID(Enumerable.Repeat((byte)0xFF, Constants.ID_LENGTH_BYTES).ToArray());
        }

		public static ID operator ^(ID a, ID b)
		{
            return new ID(a.id ^ b.id);
		}

		public static bool operator <(ID a, ID b)
		{
            return a.id < b.id;
		}

		public static bool operator >(ID a, ID b)
		{
            return a.id > b.id;
		}

		public static bool operator ==(ID a, ID b)
		{
			Validate.IsFalse(ReferenceEquals(a, null), "ID a cannot be null.");
			Validate.IsFalse(ReferenceEquals(b, null), "ID b cannot be null.");

            return a.id == b.id;
		}

		public static bool operator !=(ID a, ID b)
		{
			return !(a == b); // Already have that
		}

		public static ID operator <<(ID idobj, int count)
		{
            return new ID(idobj.id << count);
		}

        public static ID operator >>(ID idobj, int count)
        {
            return new ID(idobj.id >> count);
        }
    }
}
