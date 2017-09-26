using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace Clifton.Kademlia.Protocols
{
    // ==========================

    public class TcpSubnetProtocol : IProtocol
    {
#if DEBUG       // for unit tests
        public bool Responds { get; set; }
#endif

        public int Subnet { get { return subnet; } }

        protected string url;
        protected int port;
        protected int subnet;

        public TcpSubnetProtocol(string url, int port, int subnet)
        {
            this.url = url;
            this.port = port;
            this.subnet = subnet;

#if DEBUG
            Responds = true;
#endif
        }

        public (List<Contact> contacts, RpcError error) FindNode(Contact sender, ID key)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            bool timeoutError;

            var ret = RestCall.Post<FindNodeResponse, ErrorResponse>(url + ":" + port + "//FindNode",
                new FindNodeRequest() { Subnet = subnet, Sender = sender.ID.Value, Key = key.Value, RandomID = id.Value }, out error, out timeoutError);

            return (ret?.Contacts?.Select(val => new Contact(null, new ID(val))).ToList() ?? EmptyContactList(), GetRpcError(id, ret, timeoutError, error));
        }

        /// <summary>
        /// Attempt to find the value in the peer network.
        /// </summary>
        /// <returns>A null contact list is acceptable here as it is a valid return if the value is found.
        /// The caller is responsible for checking the timeoutError flag to make sure null contacts is not
        /// the result of a timeout error.</returns>
        public (List<Contact> contacts, string val, RpcError error) FindValue(Contact sender, ID key)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            bool timeoutError;

            var ret = RestCall.Post<FindValueResponse, ErrorResponse>(url + ":" + port + "//FindValue",
                new FindValueRequest() { Subnet = subnet, Sender = sender.ID.Value, Key = key.Value, RandomID = id.Value }, out error, out timeoutError);

            return (ret?.Contacts?.Select(val => new Contact(null, new ID(val))).ToList(), ret.Value, GetRpcError(id, ret, timeoutError, error));
        }

        public RpcError Ping(Contact sender)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            bool timeoutError;

            var ret = RestCall.Post<FindValueResponse, ErrorResponse>(url + ":" + port + "//Ping",
                new PingRequest() { Subnet = subnet, Sender = sender.ID.Value, RandomID = id.Value}, out error, out timeoutError);

            return GetRpcError(id, ret, timeoutError, error);
        }

        public RpcError Store(Contact sender, ID key, string val, bool isCached = false, int expirationTimeSec = 0)
        {
            ErrorResponse error;
            ID id = ID.RandomID;
            bool timeoutError;

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
                    }, out error, out timeoutError);

            return GetRpcError(id, ret, timeoutError, error);
        }

        protected RpcError GetRpcError(ID id, BaseResponse resp, bool timeoutError, ErrorResponse peerError)
        {
            return new RpcError() { IDMismatchError = id != resp.RandomID, TimeoutError = timeoutError, PeerError = peerError != null, PeerErrorMessage = peerError?.ErrorMessage };
        }

        protected List<Contact> EmptyContactList()
        {
            return new List<Contact>();
        }
    }
}
