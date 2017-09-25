using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Clifton.Kademlia.Protocols
{
    public class TcpSubnetServer
    {
        protected Dictionary<int, IProtocol> subnets;
        protected HttpListener listener;
        protected string url;
        protected int port;

        /// <summary>
        /// Instantiate the server, listening on the specified url and port.
        /// </summary>
        /// <param name="url">Of the form http://127.0.0.1 or https or domain name.  No trailing forward slash.</param>
        /// <param name="port">The port number.</param>
        public TcpSubnetServer(string url, int port)
        {
            this.url = url;
            this.port = port;
            subnets = new Dictionary<int, IProtocol>();
        }

        public void RegisterProtocol(int subnet, IProtocol protocol)
        {
            subnets[subnet] = protocol;
        }

        public void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url + ":" + port + "/");
            listener.Start();
            Task.Run(() => WaitForConnection(listener));
        }

        protected virtual void WaitForConnection(object objListener)
        {
            HttpListener listener = (HttpListener)objListener;

            while (true)
            {
                // Wait for a connection.  Return to caller while we wait.
                HttpListenerContext context = listener.GetContext();
                ProcessRequest(context);
            }
        }

        protected virtual void ProcessRequest(HttpListenerContext context)
        {
            string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
            var req = JsonConvert.DeserializeObject<BaseSubnetRequest>(data);
        }
    }
}