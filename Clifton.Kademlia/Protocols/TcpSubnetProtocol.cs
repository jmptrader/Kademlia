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
        public string RandomID { get; set; }
        public string Sender { get; set; }

        public BaseRequest()
        {
            RandomID = ID.RandomID.ToString();
        }
    }

    public abstract class BaseSubnetRequest : BaseRequest
    {
        public int Subnet { get; set; }
    }

    public class FindNodeRequest : BaseSubnetRequest
    {
        public string Key { get; set; }
    }

    public class FindValueRequest : BaseSubnetRequest
    {
        public string Key { get; set; }
    }

    public class PingRequest : BaseSubnetRequest { }

    public class StoreRequest : BaseSubnetRequest
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsCached { get; set; }
        public int ExpirationTimeSec { get; set; }
    }

    // ==========================

    public abstract class BaseResponse
    {
        public BigInteger RandomID { get; set; }
    }

    public class FindNodeResponse : BaseResponse
    {
        public List<Contact> Contacts { get; set; }
    }

    public class FindValueResponse : BaseResponse
    {
        public List<Contact> Contacts { get; set; }
        public string Value { get; set; }
    }

    public class PingResponse : BaseResponse { }

    public class StoreResponse : BaseResponse { }

    public class TcpSubnetProtocol : IProtocol
    {
        public int Subnet { get; set; }
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
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindNodeResponse>(url + ":" + port + "//FindNode",
                JsonConvert.SerializeObject(new FindNodeRequest() { Subnet = subnet, Sender = sender.ID.ToString(), Key = key.ToString()}));

            return ret.Contacts;
        }

        public (List<Contact> contacts, string val) FindValue(Contact sender, ID key)
        {
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindValueResponse>(url + ":" + port + "//FindValue",
                JsonConvert.SerializeObject(new FindValueRequest() { Subnet = subnet, Sender = sender.ID.ToString(), Key = key.ToString()}));

            return (ret.Contacts, ret.Value);

        }

        public bool Ping(Contact sender)
        {
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindValueResponse>(url + ":" + port + "//Ping",
                JsonConvert.SerializeObject(new PingRequest() { Subnet = subnet, Sender = sender.ID.ToString()}));

            return true;
        }

        public void Store(Contact sender, ID key, string val, bool isCached = false, int expirationTimeSec = 0)
        {
            ID id = ID.RandomID;
            var ret = RestCall.Post<FindValueResponse>(url + ":" + port + "//Store",
                JsonConvert.SerializeObject(
                    new StoreRequest()
                    {
                        Subnet = subnet,
                        Sender = sender.ID.ToString(),
                        Key = key.ToString(),
                        Value = val,
                        IsCached = isCached,
                        ExpirationTimeSec = expirationTimeSec,
                        RandomID = ID.RandomID.ToString()
                    }));
        }
    }
}
