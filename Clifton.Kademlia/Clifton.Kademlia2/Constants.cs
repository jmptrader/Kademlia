namespace Clifton.Kademlia
{
	public static class Constants
	{
		public const int B = 5;
		public const int K = 20;
		public const int ID_LENGTH_BYTES = 20;
		public const int ID_LENGTH_BITS = 160;

        public const int MAX_THREADS = 20;

#if DEBUG       // For unit tests
        public const int ALPHA = 20;
        public const double BUCKET_REFRESH_INTERVAL = 60 * 60 * 1000;       // every hour.
        public const double KEY_VALUE_REPUBLISH_INTERVAL = 60 * 60 * 1000;       // every hour.
        public const double KEY_VALUE_EXPIRE_INTERVAL = 60 * 60 * 1000;       // every hour.
        public const double ORIGINATOR_REPUBLISH_INTERVAL = 24 * 60 * 60 * 1000;       // every 24 hours in milliseconds.
        public const int EXPIRATION_TIME_SECONDS = 24 * 60 * 60;                // every 24 hours in seconds.
#else
        public const int ALPHA = 3;
        public const double BUCKET_REFRESH_INTERVAL = 60 * 60 * 1000;       // every hour.
        public const double KEY_VALUE_REPUBLISH_INTERVAL = 60 * 60 * 1000;       // every hour.
        public const double KEY_VALUE_EXPIRE_INTERVAL = 60 * 60 * 1000;       // every hour.
        public const double ORIGINATOR_REPUBLISH_INTERVAL = 24 * 60 * 60 * 1000;       // every 24 hours in milliseconds.
        public const int EXPIRATION_TIME_SECONDS = 24 * 60 * 60;                // every 24 hours in seconds.
#endif
    }
}
