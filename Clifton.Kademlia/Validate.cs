using System;

namespace Clifton.Kademlia
{
	public static class Validate
	{
		public static void IsTrue(bool b, string errorMessage)
		{
			if (!b)
			{
				throw new Exception(errorMessage);
			}
		}

		public static void IsFalse(bool b, string errorMessage)
		{
			if (b)
			{
				throw new Exception(errorMessage);
			}
		}
	}
}
