using System;

namespace Clifton.Kademlia
{
    public class IDLengthException : Exception
    {
        public IDLengthException() { }
        public IDLengthException(string msg) : base(msg) { }
    }

    public class TooManyContactsException : Exception
    {
        public TooManyContactsException() { }
        public TooManyContactsException(string msg) : base(msg) { }
    }

	public class OurNodeCannotBeAContactException : Exception
	{
		public OurNodeCannotBeAContactException() { }
		public OurNodeCannotBeAContactException(string msg) : base(msg) { }
	}
}
