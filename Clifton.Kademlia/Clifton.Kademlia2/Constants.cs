namespace Clifton.Kademlia
{
	public static class Constants
	{
		public const int B = 5;
		public const int K = 20;
		public const int ID_LENGTH_BYTES = 20;
		public const int ID_LENGTH_BITS = 160;

#if DEBUG       // For unit tests
        public const int ALPHA = 20;
#else
        public const int ALPHA = 3;
#endif
    }
}
