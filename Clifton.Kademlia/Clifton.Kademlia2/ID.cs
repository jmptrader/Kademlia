using System.Numerics;

namespace Clifton.Kademlia
{
    public class ID
    {
#if DEBUG       // For unit testing.
        public BigInteger Value { get { return id; } }
#endif

        protected BigInteger id;

        /// <summary>
        /// Construct the ID from a byte array.
        /// </summary>
        public ID(byte[] data)
        {
            IDInit(data);
        }

        /// <summary>
        /// Construct the ID from another BigInteger value.
        /// </summary>
        public ID(BigInteger bi)
        {
            id = bi;
        }

        /// <summary>
        /// Initialize the ID from a byte array, appending a 0 to force unsigned values.
        /// </summary>
        protected void IDInit(byte[] data)
        {
            Validate.IsTrue<IDLengthException>(data.Length == Constants.ID_LENGTH_BYTES, "ID must be " + Constants.ID_LENGTH_BYTES + " bytes in length.");
            id = new BigInteger(data.Append0());
        }
    }
}
