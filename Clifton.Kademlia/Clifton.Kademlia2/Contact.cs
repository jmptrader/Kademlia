using System;

namespace Clifton.Kademlia
{
    public class Contact
    {
#if DEBUG       // For unit testing
        public IProtocol Protocol { get; set; }
#else
        public IProtocol Protocol { get; protected set; }
#endif

        public DateTime LastSeen { get; protected set; }
        public ID ID { get; protected set; }

        /// <summary>
        /// Initialize a contact with its protocol and ID.
        /// </summary>
        public Contact(IProtocol protocol, ID contactID)
        {
            Protocol = protocol;
            ID = contactID;
            Touch();
        }

        /// <summary>
        /// Update the fact that we've just seen this contact.
        /// </summary>
        public void Touch()
        {
            LastSeen = DateTime.Now;
        }
    }
}
