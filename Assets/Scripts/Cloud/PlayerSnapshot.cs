using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;

public class UUID
{
    private ulong m_UUID;

    public UUID()
    {
        m_UUID = GenerateUUID();
    }

    public UUID(ulong uuid)
    {
        m_UUID = uuid;
    }

    public UUID(UUID other)
    {
        m_UUID = other.m_UUID;
    }

    public static implicit operator ulong(UUID uuid)
    {
        return uuid.m_UUID;
    }

	// Since RandomNumberGenerator implementation spesific it doesn't guarantee low-collision probability 
	// Depending on use case we might consider Xoshiro256starstar or mt19937_64 for client-side generation
	// Another option if non-colliding UUID's is must we can get from SQL Database.
	private static ThreadLocal<byte[]> m_RngBuffer = new ThreadLocal<byte[]>(() => new byte[8]);
    private ulong GenerateUUID()
    {
        RandomNumberGenerator.Fill(m_RngBuffer.Value);
        return BitConverter.ToUInt64(m_RngBuffer.Value, 0);
    }

    public override int GetHashCode()
    {
        return m_UUID.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is UUID other)
        {
            return m_UUID == other.m_UUID;
        }
        return false;
    }
}


public class InventoryItem
{
    public UUID ItemGuid;
    public string Name;
    public int Count;
}

public class PlayerSnapshot
{
    public string PlayerName;
    public string PlayerDisplayName;
    public List<InventoryItem> Item;

    public PlayerSnapshot()
    {
        Item = new List<InventoryItem>();
    }
}


public class Snapshot
{
    public static PlayerSnapshot CurrentSnapshoot;
}

