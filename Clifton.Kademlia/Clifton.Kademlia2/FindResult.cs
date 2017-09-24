using System.Collections.Generic;

namespace Clifton.Kademlia
{
    public class FindResult
    {
        public bool Found { get; set; }
        public List<Contact> FoundContacts { get; set; }
        public Contact FoundBy { get; set; }
        public string FoundValue { get; set; }
    }
}
