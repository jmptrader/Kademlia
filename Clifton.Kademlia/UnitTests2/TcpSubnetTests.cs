using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Kademlia;
using Clifton.Kademlia.Protocols;

namespace UnitTests2
{
    [TestClass]
    public class TcpSubnetTests
    {
        [TestMethod]
        public void PingRouteTest()
        {
            string localIP = "http://127.0.0.1";
            int port = 2720;

            TcpSubnetServer server = new TcpSubnetServer(localIP, port);
            TcpSubnetProtocol p1 = new TcpSubnetProtocol(localIP, port, 1);
            TcpSubnetProtocol p2 = new TcpSubnetProtocol(localIP, port, 2);
            server.RegisterProtocol(p1.Subnet, p1);
            server.RegisterProtocol(p2.Subnet, p2);
            server.Start();

            Contact sender = new Contact(p1, ID.RandomID);
            p2.Ping(sender);
        }
    }
}
