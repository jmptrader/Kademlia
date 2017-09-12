namespace Clifton.Kademlia
{
    public class Router
    {
#if DEBUG       // for unit testing
        public Node Node { get { return node; } }
#endif

        protected Node node;

        public Router(Node node)
        {
            this.node = node;
        }
    }
}
