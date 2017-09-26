using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Kademlia
{
    public class RpcError
    {
        public bool HasError { get { return TimeoutError || IDMismatchError || PeerError; } }
        public bool TimeoutError { get; set; }
        public bool IDMismatchError { get; set; }
        public bool PeerError { get; set; }
        public string PeerErrorMessage { get; set; }
    }
}
