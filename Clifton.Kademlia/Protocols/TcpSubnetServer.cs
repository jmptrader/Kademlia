using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Clifton.Kademlia.Protocols
{
    public class TcpSubnetServer
    {
        protected Dictionary<int, Node> subnets;
        protected HttpListener listener;
        protected string url;
        protected int port;
        protected bool running;

        protected Dictionary<string, Type> routePackets = new Dictionary<string, Type>
        {
            {"//Ping", typeof(PingRequest) },
            {"//Store", typeof(StoreRequest) },
            {"//FindNode", typeof(FindNodeRequest) },
            {"//FindValue", typeof(FindValueRequest) },
        };

        /// <summary>
        /// Instantiate the server, listening on the specified url and port.
        /// </summary>
        /// <param name="url">Of the form http://127.0.0.1 or https or domain name.  No trailing forward slash.</param>
        /// <param name="port">The port number.</param>
        public TcpSubnetServer(string url, int port)
        {
            this.url = url;
            this.port = port;
            subnets = new Dictionary<int, Node>();
        }

        public void RegisterProtocol(int subnet, Node node)
        {
            subnets[subnet] = node;
        }

        public void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url + ":" + port + "/");
            listener.Start();
            running = true;
            Task.Run(() => WaitForConnection());
        }

        public void Stop()
        {
            running = false;
            listener.Stop();
        }

        protected virtual void WaitForConnection()
        {
            while (running)
            {
                // Wait for a connection.  Return to caller while we wait.
                HttpListenerContext context = listener.GetContext();
                ProcessRequest(context);
            }
        }

        protected virtual void ProcessRequest(HttpListenerContext context)
        {
            string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();

            if (context.Request.HttpMethod == "POST")
            {
                Type request;
                string path = context.Request.RawUrl;

                if (routePackets.TryGetValue(path, out request))
                {
                    BaseSubnetRequest req = (BaseSubnetRequest)JsonConvert.DeserializeObject(data, request);
                    Node node;

                    if (subnets.TryGetValue(req.Subnet, out node))
                    {
                        string methodName = path.Substring(2);      // Remove "//"

                        try
                        {
                            var sender = new Contact(null, new ID(req.Sender));

                            // Ugh.
                            switch (methodName)
                            {
                                case "Ping":
                                    node.Ping(sender);
                                    SendResponse(context, new PingResponse() { RandomID = req.RandomID });
                                    break;

                                case "Store":
                                    node.Store(sender, new ID(((StoreRequest)req).Key), ((StoreRequest)req).Value, ((StoreRequest)req).IsCached, ((StoreRequest)req).ExpirationTimeSec);
                                    SendResponse(context, new StoreResponse() { RandomID = req.RandomID });
                                    break;

                                case "FindNode":
                                    List<Contact> contacts = node.FindNode(sender, new ID(((FindNodeRequest)req).Key)).contacts;
                                    SendResponse(context, new FindNodeResponse() { Contacts = contacts.Select(c => c.ID.Value).ToList(), RandomID = req.RandomID });
                                    break;

                                case "FindValue":
                                    var ret = node.FindValue(sender, new ID(((FindValueRequest)req).Key));
                                    SendResponse(context, new FindValueResponse() { Contacts = ret.contacts?.Select(c => c.ID.Value)?.ToList(), RandomID = req.RandomID, Value = ret.val });
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            SendErrorResponse(context, new ErrorResponse() { ErrorMessage = ex.Message });
                        }
                    }
                    else
                    {
                        SendErrorResponse(context, new ErrorResponse() { ErrorMessage = "Subnet node not found." });
                    }
                }
                else
                {
                    SendErrorResponse(context, new ErrorResponse() { ErrorMessage = "Method not recognmized." });
                }
            }

            context.Response.Close();
        }

        protected void SendResponse(HttpListenerContext context, BaseResponse resp)
        {
            context.Response.StatusCode = 200;
            SendResponseInternal(context, resp);
        }

        protected void SendErrorResponse(HttpListenerContext context, ErrorResponse resp)
        {
            context.Response.StatusCode = 400;
            SendResponseInternal(context, resp);
        }

        private void SendResponseInternal(HttpListenerContext context, BaseResponse resp)
        {
            context.Response.ContentType = "text/text";
            context.Response.ContentEncoding = Encoding.UTF8;
            byte[] byteData = JsonConvert.SerializeObject(resp).to_Utf8();

            context.Response.OutputStream.Write(byteData, 0, byteData.Length);
        }
    }
}