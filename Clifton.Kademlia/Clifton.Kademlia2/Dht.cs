using System.Collections.Generic;

namespace Clifton.Kademlia
{
    public class Dht
    {
#if DEBUG       // for unit testing
        public Router Router { get { return router; } }
#endif

        protected Router router;
        protected IStorage storage;
        protected IProtocol protocol;
        protected Node node;

        public Dht(ID id, IProtocol protocol, IStorage storage)
        {
            this.storage = storage;
            node = new Node(new Contact(protocol, id), storage);
            router = new Router(node);
        }

        public void Store(ID key, string val)
        {
            // We're storing to ourselves as well as k closer contacts.
            storage.Set(key, val);
            List<Contact> contacts = router.Lookup(key, router.RpcFindNodes).contacts;
            contacts.ForEach(c => c.Protocol.Store(node.OurContact, key, val));
        }

        public (bool found, List<Contact> contacts, string val) FindValue(ID key)
        {
            string ourval;

            // If we have it, return with our value.
            if (storage.TryGetValue(key, out ourval))
            {
                return (true, null, ourval);
            }

            var (contacts, val) = router.Lookup(key, router.RpcFindValue);
            var found = contacts == null;

            if (found)
            {
                node.Cache(key, val);
            }

            return (found, contacts, val);
        }
    }
}
