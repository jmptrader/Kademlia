using System.Collections.Generic;

using Clifton.Kademlia;

namespace UnitTests2
{
    public class VirtualProtocol : IProtocol
    {
        public Node Node { get; set; }
        public bool Responds { get; set; }

        /// <summary>
        /// For unit testing with deferred node setup.
        /// </summary>
        public VirtualProtocol(bool responds = true)
        {
            Responds = responds;
        }

        /// <summary>
        /// Register the in-memory node with our virtual protocol.
        /// </summary>
        public VirtualProtocol(Node node, bool responds = true)
        {
            Node = node;
            Responds = responds;
        }

        public bool Ping(Contact sender)
        {
            // Ping still adds/updates the sender's contact.
            if (Responds)
            {
                Node.Ping(sender);
            }

            return Responds;
        }

        /// <summary>
        /// Get the list of contacts for this node closest to the key.
        /// </summary>
        public List<Contact> FindNode(Contact sender, ID key)
        {
            return Node.FindNode(sender, key).contacts;
        }

        /// <summary>
        /// Returns either contacts or null if the value is found.
        /// </summary>
        public (List<Contact> contacts, string val) FindValue(Contact sender, ID key)
        {
            return Node.FindValue(sender, key);
        }

        /// <summary>
        /// Stores the key-value on the remote peer.
        /// </summary>
        public void Store(Contact sender, ID key, string val, bool isCached = false, int expTimeSec = 0)
        {
            Node.Store(sender, key, val, isCached, expTimeSec);
        }
    }
}
