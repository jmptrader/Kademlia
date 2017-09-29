﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace Clifton.Kademlia.Common
{
    //public interface IKBucket
    //{
    //    BigInteger Low { get; }
    //    BigInteger High { get; }
    //}

    public interface IDht
    {
        void DelayEviction(Contact toEvict, Contact toReplace);
        void AddToPending(Contact pending);
    }

    public interface IBucketList
    {
        List<KBucket> Buckets { get; }
        IDht Dht { get; set; }
        void AddContact(Contact contact);
        KBucket GetKBucket(ID otherID);
        List<Contact> GetCloseContacts(ID key, ID exclude);
    }

    public interface IProtocol
    {
        RpcError Ping(Contact sender);
        (List<Contact> contacts, RpcError error) FindNode(Contact sender, ID key);
        (List<Contact> contacts, string val, RpcError error) FindValue(Contact sender, ID key);
        RpcError Store(Contact sender, ID key, string val, bool isCached = false, int expirationTimeSec = 0);
    }

    public interface INode
    {
        Contact OurContact { get; }
        IBucketList BucketList { get; }
    }

    public interface IStorage : IEnumerable<BigInteger>
    {
        bool HasValues { get; }
        bool Contains(ID key);
        bool TryGetValue(ID key, out string val);
        string Get(ID key);
        string Get(BigInteger key);
        DateTime GetTimeStamp(BigInteger key);
        void Set(ID key, string value, int expirationTimeSec = 0);
        int GetExpirationTimeSec(BigInteger key);
        void Remove(BigInteger key);

        /// <summary>
        /// Updates the republish timestamp.
        /// </summary>
        void Touch(BigInteger key);
    }
}
