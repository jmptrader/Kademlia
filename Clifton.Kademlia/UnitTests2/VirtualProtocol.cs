using System.Collections.Generic;

using Clifton.Kademlia;

namespace UnitTests2
{
    public class VirtualProtocol : IProtocol
    {
#if DEBUG
        public Node Node { get { return node; } }
#endif

        protected Node node;

        /// <summary>
        /// Register the in-memory node with our virtual protocol.
        /// </summary>
        public VirtualProtocol(Node node)
        {
            this.node = node;
        }

        /// <summary>
        /// Get the list of contacts for this node closest to the key.
        /// </summary>
        public List<Contact> FindNode(Contact sender, ID key)
        {
            return node.FindNode(sender, key).contacts;
        }
    }
}
