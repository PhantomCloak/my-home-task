using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class UUID
{
    // Since RandomNumberGenerator implementation spesific it doesn't guarantee low-collision probability
    // Depending on use case we might consider Xoshiro256starstar or mt19937_64 for client-side generation
    // Another option if non-colliding UUID's is must we can get from SQL Database or Cloud function.
    private static ThreadLocal<byte[]> m_RngBuffer = new ThreadLocal<byte[]>(() => new byte[8]);

    public static ulong GenerateUUID()
    {
        RandomNumberGenerator.Fill(m_RngBuffer.Value);
        return BitConverter.ToUInt64(m_RngBuffer.Value, 0);
    }
}

[Serializable]
public class InventoryItem
{
    public ulong ItemGuid;
    public string Name;
    public int Count;

    public InventoryItem(string name, int count)
    {
        ItemGuid = UUID.GenerateUUID();
        Name = name;
        Count = count;
    }
}

[Serializable]
public class PlayerStats
{
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
                Debug.LogError(
                    $"Snapshot fetch request failed: {e.Message} - {e.StackTrace}. Inner Exception: {e.InnerException.ToString()}"
                );
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
                Debug.LogError(
                    $"Snapshot exist request failed: {e.Message} - {e.StackTrace}. Inner Exception: {e.InnerException.ToString()}"
                );
                throw;
            }
        });
    }

    public static async Task<PlayerSnapshot> GetSnapshotOtherAsync(string userId)
    {
        PlayerSnapshot result = null;
        try
        {
            result = await CloudApi.GetVariableCloudAsync<PlayerSnapshot>(
                CloudConstants.SnapshotKey,
                userId
            );
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
            result = await CloudApi.GetVariableCloudAsync<PlayerSnapshot>(
                CloudConstants.SnapshotKey
            );
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
