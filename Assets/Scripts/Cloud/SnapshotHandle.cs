using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum AcquireType
{
    ReadOnly,
    ReadWrite
}

public class SnapshotHandle : IDisposable
{
    public PlayerSnapshot Value;
    private AcquireType m_Type;

    private static Queue<SnapshotHandle> m_SnapshotSyncObj = new Queue<SnapshotHandle>();

    private static int m_WriteHandleIndex = 0;
    private static float m_LastSyncTime = -1;

    private static Task m_SyncTask;

    public SnapshotHandle(AcquireType type)
    {
        if (m_WriteHandleIndex > 0 && type == AcquireType.ReadWrite)
        {
            throw new InvalidOperationException("Cannot create a nested ReadWrite handle.");
        }

        m_Type = type;
        Value = ObjectCopier.Clone<PlayerSnapshot>(Snapshot.CurrentSnapshoot);

        if (m_Type == AcquireType.ReadWrite)
            m_WriteHandleIndex++;
    }

    public void Dispose()
    {
        if (m_Type == AcquireType.ReadWrite)
            m_WriteHandleIndex--;

        if (m_Type == AcquireType.ReadOnly)
        {
            if (ObjectCopier.IsDifferent<PlayerSnapshot>(Snapshot.CurrentSnapshoot, Value))
                throw new Exception("PlayerSnapshot has marked readonly but tried to write!");

            return;
        }

        Snapshot.CurrentSnapshoot = Value;
        m_SnapshotSyncObj.Enqueue(this);

        if (m_SyncTask == null)
        {
            m_SyncTask = Task.Run(() => SyncTaskUpdate());
        }
    }

    bool m_PushImmidietly = false;
    static double m_AccumulatedTime = -1;
    private async Task SyncTaskUpdate()
    {
        while (true)
        {
            if (m_AccumulatedTime != -1 && m_AccumulatedTime > 1000f)
            {
                while (m_SnapshotSyncObj.Count > 0)
                {
                    var instanceTuple = m_SnapshotSyncObj.Dequeue();
                    await CloudApi.SetVariableCloudAsync(nameof(PlayerSnapshot), instanceTuple.Value);
                }
                m_AccumulatedTime = 0;
            }

            await Task.Delay(16);
            // Let's put our faith in the task scheduler for now :)
            m_AccumulatedTime += 16;
        }
    }

    private async Task ForceSync()
    {
        while (m_SnapshotSyncObj.Count > 0)
        {
            while (m_SnapshotSyncObj.Count > 0)
            {
                var instanceTuple = m_SnapshotSyncObj.Dequeue();
                await CloudApi.SetVariableCloudAsync(nameof(PlayerSnapshot), instanceTuple.Value);
            }
        }
    }
}
