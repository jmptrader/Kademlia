using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
    public class Node
    {
        public Contact OurContact { get { return ourContact; } }
        public BucketList BucketList { get { return bucketList; } }
        public IStorage Storage { get { return storage; } }

        protected Contact ourContact;
        protected BucketList bucketList;
        protected IStorage storage;

        protected Node()
        {
        }

        public Node(Contact contact, IStorage storage)
        {
            ourContact = contact;
            bucketList = new BucketList(contact);
            this.storage = storage;
        }

        /// <summary>
        /// Someone is pinging us.  Register the contact and respond.
        /// </summary>
        public Contact Ping(Contact sender)
        {
            Validate.IsFalse<SendingQueryToSelfException>(sender.ID.Value == ourContact.ID.Value, "Sender should not be ourself!");
            SendKeyValuesToNewContact(sender);
            bucketList.AddContact(sender);

            return ourContact;
        }

        /// <summary>
        /// Store a key-value pair in our storage space, updating the contact if it's not us.
        /// </summary>
        public void Store(Contact sender, ID key, string val)
        {
            Validate.IsFalse<SendingQueryToSelfException>(sender.ID.Value == ourContact.ID.Value, "Sender should not be ourself!");
            SendKeyValuesToNewContact(sender);
            bucketList.AddContact(sender);

            storage.Set(key, val);
        }

        /// <summary>
        /// From the spec: FindNode takes a 160-bit ID as an argument. The recipient of the RPC returns (IP address, UDP port, Node ID) triples 
        /// for the k nodes it knows about closest to the target ID. These triples can come from a single k-bucket, or they may come from 
        /// multiple k-buckets if the closest k-bucket is not full. In any case, the RPC recipient must return k items (unless there are 
        /// fewer than k nodes in all its k-buckets combined, in which case it returns every node it knows about).
        /// </summary>
        /// <returns></returns>
        public (List<Contact> contacts, string val) FindNode(Contact sender, ID key)
        {
            Validate.IsFalse<SendingQueryToSelfException>(sender.ID.Value == ourContact.ID.Value, "Sender should not be ourself!");
            SendKeyValuesToNewContact(sender);
            bucketList.AddContact(sender);

            // Exclude sender.
            var contacts = bucketList.GetCloseContacts(key, sender.ID);

            return (contacts, null);
        }

        /// <summary>
        /// Returns either a list of close contacts or a the value, if the node's storage contains the value for the key.
        /// </summary>
        public (List<Contact> contacts, string val) FindValue(Contact sender, ID key)
        {
            Validate.IsFalse<SendingQueryToSelfException>(sender.ID.Value == ourContact.ID.Value, "Sender should not be ourself!");
            SendKeyValuesToNewContact(sender);
            bucketList.AddContact(sender);

            if (storage.Contains(key))
            {
                return (null, storage.Get(key));
            }
            else
            {
                // Exclude sender.
                return (bucketList.GetCloseContacts(key, sender.ID), null);
            }
        }

        public void Cache(ID key, string val)
        {
            storage.Set(key, val);
        }

        protected void SendKeyValuesToNewContact(Contact sender)
        {
            // If we have a new contact...
            if (!bucketList.ContactExists(sender))
            {
                // and our distance to the key < any other contact's distance to the key...
                storage.ForEach(k =>
                {
                    var contacts = bucketList.Buckets.SelectMany(b => b.Contacts);

                    if (contacts.Count() > 0)
                    {
                        var distance = contacts.Min(c => k ^ c.ID.Value);

                        if ((k ^ ourContact.ID.Value) < distance)
                        {
                            sender.Protocol.Store(ourContact, new ID(k), storage.Get(k));   // send it to the new contact.
                        }
                    }
                });
            }
        }
    }
}
