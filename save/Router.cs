namespace Clifton.Kademlia
{
    public class Router
    {
#if DEBUG       // for unit testing
        public ID ID { get { return id; } }
#endif

        protected ID id;
    }
}
