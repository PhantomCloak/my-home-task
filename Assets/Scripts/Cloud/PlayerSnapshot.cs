using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
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


public class UUIDConverter : JsonConverter<UUID>
{
    public override void WriteJson(JsonWriter writer, UUID value, JsonSerializer serializer)
    {
        writer.WriteValue((ulong)value);
    }

    public override UUID ReadJson(JsonReader reader, Type objectType, UUID existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType == JsonToken.Integer)
        {
            ulong uuid = serializer.Deserialize<ulong>(reader);
            return new UUID(uuid);
        }

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing UUID.");
    }
}


[Serializable]
public class InventoryItem
{
    public UUID ItemGuid = new UUID();
    public string Name;
    public int Count;

	public InventoryItem(string name, int count) {
		Name = name;
		Count = count;
	}
}

[Serializable]
public class PlayerStats {
	public int LaunchCount;
}

[Serializable]
public class PlayerSnapshot
{
    public string PlayerName;
	public PlayerStats Stats;
    public List<InventoryItem> Items;

    public PlayerSnapshot()
    {
		Stats = new PlayerStats();
        Items = new List<InventoryItem>();
    }
}


public class Snapshot
{
    public static PlayerSnapshot CurrentSnapshoot;

    public static async void UpdateSnapshotAsync()
    {
        var snap = await GetSnapshotAsync();
        SetCurrentSnapshot(snap);
    }

    // Since callback came from thread-pool, the callback cannot access resources
    // that are in main thread, to circumvent the issue an intermediate step
    // introduced to redirect callbacks from non-main threads to main thread in expense of Stack Trace
    public static void UpdateSnapshot(Action<bool> onSnapshotUpdate = null)
    {
		var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onSnapshotUpdate);
        Task.Run(async () =>
        {
            try
            {
                var snap = await GetSnapshotAsync();
                SetCurrentSnapshot(snap);

                handle.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Snapshot fetch request failed: {e.Message} - {e.StackTrace}. Inner Exception: {e.InnerException.ToString()}");
                handle.Invoke(false);
            }
        });
    }

    public static void IsSnapshotExist(Action<bool> onSnapshotExistResult)
    {
		var handle = ThreadContextManager.GetSynchronizeCallbackHandler(onSnapshotExistResult);
        Task.Run(async () =>
        {
            try
            {
				var result = await IsSnapshotExistAsync();
                Debug.Log($"Snapshot exist result: {result}");
                handle.Invoke(result);
            }
            catch (Exception e)
            {
                Debug.LogError($"Snapshot exist request failed: {e.Message} - {e.StackTrace}. Inner Exception: {e.InnerException.ToString()}");
				throw;
            }
        });
    }

    public static async Task<PlayerSnapshot> GetSnapshotOtherAsync(string userId)
    {
        PlayerSnapshot result = null;
        try
        {
            result = await CloudApi.GetVariableCloudAsync<PlayerSnapshot>(CloudConstants.SnapshotKey, userId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Snapshot other fetch request failed: {e.Message}");
            return default;
        }

        return result;
    }

    private static async Task<PlayerSnapshot> GetSnapshotAsync()
    {
        PlayerSnapshot result = null;
        try
        {
            result = await CloudApi.GetVariableCloudAsync<PlayerSnapshot>(CloudConstants.SnapshotKey);
        }
        catch (Exception e)
        {
            Debug.LogError($"Snapshot fetch request failed: {e.Message}");
            return default;
        }

        return result;
    }

    private static async Task<bool> IsSnapshotExistAsync()
    {
        bool result = false;
        try
        {
            result = await CloudApi.IsExistVariableCloudAsync(CloudConstants.SnapshotKey);
        }
        catch (Exception e)
        {
            Debug.LogError($"Snapshot exist request failed: {e.Message}");
            return default;
        }

        return result;
    }

    private static void SetCurrentSnapshot(PlayerSnapshot snapshot)
    {
        CurrentSnapshoot = snapshot;
    }
}

