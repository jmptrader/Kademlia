using System;

namespace Clifton.Kademlia
{
    public class Dht
    {
#if DEBUG       // for unit testing
        public Router Router { get { return router; } }
#endif
        protected Router router;

    }
}
