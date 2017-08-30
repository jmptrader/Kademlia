using System;
using System.Collections.Generic;

namespace Clifton.Kademlia
{
	public class Node
	{
		// TODO: Create read-only version of these or otherwise rework to avoid getters that can change the contents.
		public Contact OurContact { get; }
		public BucketList BucketList { get { return bucketList; } }
		public IStorage Storage { get; set; }

		protected BucketList bucketList;

		protected Node()
		{
			bucketList = new BucketList();
		}

		public Node(Contact us) : this()
		{
			OurContact = us;
		}

		public Node(IAddress address, ID nodeID) : this()
		{
			OurContact = new Contact() { Address = address, NodeID = nodeID };
		}

		public void SimpleRegistration(Contact sender)
		{
			bucketList.HaveContact(OurContact.NodeID, sender, (_) => false);
		}

		public Contact Ping(Contact sender)
		{
			SimpleRegistration(sender);

			return OurContact;
		}

		public void Store(Contact sender, ID keyID, string val)
		{
			bucketList.HaveContact(OurContact.NodeID, sender, (_) => false);
			Storage.Set(keyID.ToString(), val);
		}

		/// <summary>
		/// From the spec: FindNode takes a 160-bit ID as an argument. The recipient of the RPC returns (IP address, UDP port, Node ID) triples 
		/// for the k nodes it knows about closest to the target ID. These triples can come from a single k-bucket, or they may come from 
		/// multiple k-buckets if the closest k-bucket is not full. In any case, the RPC recipient must return k items (unless there are 
		/// fewer than k nodes in all its k-buckets combined, in which case it returns every node it knows about).
		/// </summary>
		/// <returns></returns>
		public (List<Contact> nodes, string val) FindNode(Contact sender, ID toFind)
		{
			bucketList.HaveContact(OurContact.NodeID, sender, (_) => false);
			List<Contact> contacts = bucketList.GetCloseContacts(toFind, sender.NodeID);

			return (contacts, null);
		}

		/// <summary>
		/// Returns either a list of close contacts or a the value, if the node's storage contains the value for the key.
		/// </summary>
		public (List<Contact> nodes, string val) FindValue(Contact sender, ID keyID)
		{
			List<Contact> contacts = null;
			string val = null;

			bucketList.HaveContact(OurContact.NodeID, sender, (_) => false);

			if (!Storage.TryGetValue(keyID.ToString(), out val))
			{
				contacts = bucketList.GetCloseContacts(keyID, sender.NodeID);
			}

			return (contacts, val);
		}
	}
}
