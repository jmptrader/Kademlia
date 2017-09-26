using System.Numerics;

namespace Clifton.Kademlia
{
    /// <summary>
    /// For passing to Node handlers with common parameters.
    /// </summary>
    public class CommonRequest
    {
        public BigInteger RandomID { get; set; }
        public BigInteger Sender { get; set; }
        public BigInteger Key { get; set; }
        public string Value { get; set; }
        public bool IsCached { get; set; }
        public int ExpirationTimeSec { get; set; }
    }
}
