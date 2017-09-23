using System;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.Kademlia
{
    public class Node
    {
        public Contact OurContact { get { return ourContact; } }
        public BucketList BucketList { get { return bucketList; } }
        public IStorage Storage { get { return storage; } }
        public IStorage CacheStorage { get { return cacheStorage; } }

        protected Contact ourContact;
        protected BucketList bucketList;
        protected IStorage storage;
        protected IStorage cacheStorage;

        protected Node()
        {
        }

        /// <summary>
        /// If cache storage is not explicity provided, we use an in-memory virtual storage.
        /// </summary>
        public Node(Contact contact, IStorage storage, IStorage cacheStorage = null)
        {
            ourContact = contact;
            bucketList = new BucketList(contact);
            this.storage = storage;
            this.cacheStorage = cacheStorage;

            if (cacheStorage == null)
            {
                this.cacheStorage = new VirtualStorage();
            }
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
        /// Store a key-value pair in the republish or cache storage, updating the contact if it's not us.
        /// </summary>
        public void Store(Contact sender, ID key, string val, bool isCached = false, int expirationTimeSec = 0)
        {
            Validate.IsFalse<SendingQueryToSelfException>(sender.ID.Value == ourContact.ID.Value, "Sender should not be ourself!");

            if (isCached)
            {
                cacheStorage.Set(key, val, expirationTimeSec);
            }
            else
            {
                SendKeyValuesToNewContact(sender);
                bucketList.AddContact(sender);

                storage.Set(key, val, Constants.EXPIRATION_TIME_SECONDS);
            }
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
            else if (CacheStorage.Contains(key))
            {
                return (null, CacheStorage.Get(key));
            }
            else
            {
                // Exclude sender.
                return (bucketList.GetCloseContacts(key, sender.ID), null);
            }
        }

#if DEBUG           // For unit testing
        public void SimpleStore(ID key, string val)
        {
            storage.Set(key, val);
        }
#endif

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
