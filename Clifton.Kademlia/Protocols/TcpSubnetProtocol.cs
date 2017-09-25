using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Clifton.Kademlia.Protocols
{
    public abstract class BaseRequest
    {
        public BigInteger RandomID { get; set; }
        public BigInteger Sender { get; set; }

        public BaseRequest()
        {
            RandomID = ID.RandomID.Value;
        }
    }

    public abstract class BaseSubnetRequest : BaseRequest
    {
        public int Subnet { get; set; }
    }

    public class FindNodeRequest : BaseSubnetRequest
    {
        public BigInteger Key { get; set; }
    }

    public class FindValueRequest : BaseSubnetRequest
    {
        public BigInteger Key { get; set; }
    }

    public class PingRequest : BaseSubnetRequest { }

    public class StoreRequest : BaseSubnetRequest
    {
        public BigInteger Key { get; set; }
        public string Value { get; set; }
        public bool IsCached { get; set; }
        public int ExpirationTimeSec { get; set; }
    }

    // ==========================

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

    public class TcpSubnetProtocol : IProtocol
    {
        public int Subnet { get { return subnet; } }

        protected string url;
        protected int port;
        protected int subnet;

        public TcpSubnetProtocol(string url, int port, int subnet)
        {
            this.url = url;
            this.port = port;
            this.subnet = subnet;
        }

        public List<Contact> FindNode(Contact sender, ID key)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindNodeResponse, ErrorResponse>(url + ":" + port + "//FindNode",
                new FindNodeRequest() { Subnet = subnet, Sender = sender.ID.Value, Key = key.Value, RandomID = id.Value }, out error);

            return ret.Contacts.Select(val => new Contact(null, new ID(val))).ToList();
        }

        public (List<Contact> contacts, string val) FindValue(Contact sender, ID key)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindValueResponse, ErrorResponse>(url + ":" + port + "//FindValue",
                new FindValueRequest() { Subnet = subnet, Sender = sender.ID.Value, Key = key.Value, RandomID = id.Value }, out error);

            Validate.IsTrue<RpcException>(error == null, error?.ErrorMessage);
            Validate.IsTrue<IDMismatchException>(id == ret.RandomID, "Peer did not respond with appropriate random ID.");

            return (ret?.Contacts?.Select(val => new Contact(null, new ID(val))).ToList(), ret.Value);
        }

        public bool Ping(Contact sender)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindValueResponse, ErrorResponse>(url + ":" + port + "//Ping",
                new PingRequest() { Subnet = subnet, Sender = sender.ID.Value, RandomID = id.Value}, out error);

            Validate.IsTrue<RpcException>(error == null, error?.ErrorMessage);
            Validate.IsTrue<IDMismatchException>(id == ret.RandomID, "Peer did not respond with appropriate random ID.");

            return true;
        }

        public void Store(Contact sender, ID key, string val, bool isCached = false, int expirationTimeSec = 0)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindValueResponse, ErrorResponse>(url + ":" + port + "//Store",
                    new StoreRequest()
                    {
                        Subnet = subnet,
                        Sender = sender.ID.Value,
                        Key = key.Value,
                        Value = val,
                        IsCached = isCached,
                        ExpirationTimeSec = expirationTimeSec,
                        RandomID = id.Value
                    }, out error);

            Validate.IsTrue<RpcException>(error == null, error?.ErrorMessage);
            Validate.IsTrue<IDMismatchException>(id == ret.RandomID, "Peer did not respond with appropriate random ID.");
        }
    }
}
