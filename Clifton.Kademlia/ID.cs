using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

// This code is heavily borrowed from here: https://github.com/zencoders/sambatyon/blob/master/Kademlia/Kademlia/ID.cs
// Credits to Dario Mazza, Sebastiano Merlino

namespace Clifton.Kademlia
{
	public class ID : IComparable
	{
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

		public int GetBucketIndex()
		{
			int idx = Constants.ID_LENGTH_BITS - 1;
			int msbyte = 0;
			int msbit = 0x80;
			bool done = false;

			while (msbyte < Constants.ID_LENGTH_BYTES && !done)
			{
				while (msbit != 0)
				{
					if ((id[msbyte] & msbit) == 0)
					{
						--idx;
						msbit >>= 1;
					}
					else
					{
						done = true;
						break;
					}
				}

				msbit = 0x80;
				++msbyte;
			}

			return idx + (!done ? 1 : 0);	// compensate for last --idx
		}

		/// <summary>
		/// This method initialize an id starting from a byte array.
		/// </summary>
		/// <param name="data">Data converted</param>
		private void IDInit(byte[] data)
		{
			if (data.Length == Constants.ID_LENGTH_BYTES)
			{
				this.id = new byte[Constants.ID_LENGTH_BYTES];
				data.CopyTo(this.id, 0); // Copy the array into us.
			}
			else
			{
				throw new Exception("An ID must be exactly " + Constants.ID_LENGTH_BYTES + " bytes.");
			}
		}

		/// <summary>
		/// Hash a string to produce an ID
		/// </summary>
		/// <param name="key">Key string to convert</param>
		/// <returns>An ID that represents the hash of the input string</returns>
		public static ID Hash(string key)
		{
			HashAlgorithm hasher = new SHA1CryptoServiceProvider(); // Keeping this around results in exceptions
			return new ID(hasher.ComputeHash(Encoding.UTF8.GetBytes(key)));
		}

		/// <summary>
		/// Method that generates an ID starting from a generic string. This is not hashing.
		/// </summary>
		/// <param name="hash">The hash string originating an ID</param>
		/// <returns>The ID generated</returns>
		public static ID FromString(string hash)
		{
			return new ID(
					Enumerable.Range(0, hash.Length)
					 .Where(x => x % 2 == 0)
					 .Select(x => Convert.ToByte(hash.Substring(x, 2), 16))
					 .ToArray()
					 );
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

			for (int i = 0; i < Constants.ID_LENGTH_BYTES; i++)
			{
				xoredData[i] = (byte)(a.id[i] ^ b.id[i]);
			}

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
			for (int i = 0; i < Constants.ID_LENGTH_BYTES; i++)
			{
				if (a.id[i] < b.id[i])
				{
					return true; // If first mismatch is a < b, a < b
				}
				else if (a.id[i] > b.id[i])
				{
					return false; // If first mismatch is a > b, a > b
				}
			}

			return false; // No mismatches
		}

		/// <summary>
		/// We need to compare these when measuring distance
		/// </summary>
		/// <param name="a">First ID to compare</param>
		/// <param name="b">Second ID to compare</param>
		/// <returns>true if a is greater than b; false otherwise</returns>
		public static bool operator >(ID a, ID b)
		{
			for (int i = 0; i < Constants.ID_LENGTH_BYTES; i++)
			{
				if (a.id[i] < b.id[i])
				{
					return false; // If first mismatch is a < b, a < b
				}
				else if (a.id[i] > b.id[i])
				{
					return true; // If first mismatch is a > b, a > b
				}
			}

			return false; // No mismatches
		}

		/// <summary>
		/// We need to compare these when measuring distance
		/// </summary>
		/// <param name="a">First ID to compare</param>
		/// <param name="b">Second ID to compare</param>
		/// <returns>true if a is equals to b; false otherwise</returns>
		public static bool operator ==(ID a, ID b)
		{
			// Handle null
			if (ReferenceEquals(a, null))
			{
				ReferenceEquals(b, null);
			}
			if (ReferenceEquals(b, null))
			{
				return false;
			}

			// Actually check
			for (int i = 0; i < Constants.ID_LENGTH_BYTES; i++)
			{
				if (a.id[i] != b.id[i])
				{ // Find the first difference
					return false;
				}
			}
			return true; // Must match
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

		/// <summary>
		/// Method used to verify if two objects are equals.
		/// </summary>
		/// <param name="obj">The object to compare to</param>
		/// <returns>true if the objects are equals.</returns>
		public override bool Equals(object obj)
		{
			if (obj is ID)
			{
				return this == (ID)obj;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Determines the least significant bit at which the given ID differs from this one, from 0 through 8 * ID_LENGTH - 1.
		/// PRECONDITION: IDs do not match.
		/// </summary>
		/// <param name="other">The ID to compare to</param>
		/// <returns>The least significant bit where can be found the difference</returns>
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
		/// Return a copy of ourselves that differs from us at the given bit and is random beyond that.
		/// </summary>
		/// <param name="bit">the bit to start to launch random bits</param>
		/// <returns>The new ID obtained</returns>
		public ID RandomizeBeyond(int bit)
		{
			byte[] randomized = new byte[Constants.ID_LENGTH_BYTES];
			id.CopyTo(randomized, 0);

			FlipBit(randomized, bit); // Invert pivot bit

			// And randomly flip the rest
			for (int i = bit + 1; i < 8 * Constants.ID_LENGTH_BYTES; i++)
			{
				if (rnd.NextDouble() < 0.5)
				{
					FlipBit(randomized, i);
				}
			}

			return new ID(randomized);
		}

		/// <summary>
		/// Flips the given bit in the byte array.
		/// Byte array must be ID_LENGTH long.
		/// </summary>
		/// <param name="data">Data to work on</param>
		/// <param name="bit">Bit used to generate the mask</param>
		private static void FlipBit(byte[] data, int bit)
		{
			int byteIndex = bit / 8;
			int byteBit = bit % 8;
			byte mask = (byte)(1 << byteBit);

			data[byteIndex] = (byte)(data[byteIndex] ^ mask); // Use a mask to flip the bit
		}

		/// <summary>
		/// Produce a random ID.
		/// </summary>
		/// <returns>random ID generated</returns>
		public static ID RandomID()
		{
			byte[] data = new byte[Constants.ID_LENGTH_BYTES];
			rnd.NextBytes(data);
			return new ID(data);
		}

		/// <summary>
		/// Turn this ID into a string.
		/// </summary>
		/// <returns>A string representation for the ID</returns>
		public override string ToString()
		{
			return Convert.ToBase64String(id);
		}

		/// <summary>
		/// Compare ourselves to an object
		/// </summary>
		/// <param name="obj">An obect to compare to</param>
		/// <returns>
		/// 1 if the ID is greater than the object, 0 if the object are equals and -1 if object is greater
		/// than this.
		/// </returns>
		public int CompareTo(object obj)
		{
			if (obj is ID)
			{
				// Compare as ID.
				if (this < (ID)obj)
				{
					return -1;
				}
				else if (this == (ID)obj)
				{
					return 0;
				}
				else
				{
					return 1;
				}
			}
			else
			{
				return 1; // We're bigger than random crap
			}
		}
	}
}
