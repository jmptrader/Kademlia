using System.Collections.Generic;
using System.Numerics;

namespace Protocols
{
    public abstract class BaseResponse
    {
        public BigInteger RandomID { get; set; }
    }

    public class ErrorResponse : BaseResponse
    {
        public string ErrorMessage { get; set; }
    }

    public class FindNodeResponse : BaseResponse
    {
        public List<BigInteger> Contacts { get; set; }
    }

    public class FindValueResponse : BaseResponse
    {
        public List<BigInteger> Contacts { get; set; }
        public string Value { get; set; }
    }

    public class PingResponse : BaseResponse { }

    public class StoreResponse : BaseResponse { }

}
