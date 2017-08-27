namespace Clifton.Kademlia
{
	public class Node
	{
		public ID NodeID { get; }

		protected BucketList bucketList;

		public Node()
		{
			NodeID = ID.RandomID();
			bucketList = new BucketList();
		}

		public Node(ID id)
		{
			NodeID = id;
		}

		public Node(byte[] id)
		{
			NodeID = new ID(id);
		}
	}
}
