using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class CloudApi
{
    public static bool IsConnected
    {
        get { return PlayFabClientAPI.IsClientLoggedIn(); }
    }

    private static string m_EntityId = string.Empty;
    public static string EntityId
    {
        get { return m_EntityId; }
        set { m_EntityId = value; }
    }

    private static string m_PlayfabId = string.Empty;
    public static string PlayFabId
    {
        get { return m_PlayfabId; }
        set { m_PlayfabId = value; }
    }

    public static async Task SetVariableCloudAsync<T>(
        string variableName,
        T obj,
        int timeoutMs = 2000
    )
    {
        var tcs = new TaskCompletionSource<bool>();
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);

        try
        {
            Debug.Log(
                $"SetVariableCloud request sending with variableName: {variableName}, obj: {obj}"
            );
            var jsonStr = JsonConvert.SerializeObject(obj);

            var request = new UpdateUserDataRequest
            {
                Data = new System.Collections.Generic.Dictionary<string, string>
                {
                    { variableName, jsonStr }
                },
                Permission = UserDataPermission.Public
            };

            PlayFabClientAPI.UpdateUserData(
                request,
                (success) =>
                {
                    tcs.SetResult(true);
                },
                (error) =>
                {
                    Debug.LogError($"Failed to update user data: {error.GenerateErrorReport()}");
                    tcs.SetException(
                        new Exception($"Failed to update user data: {error.GenerateErrorReport()}")
                    );
                }
            );

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            if (completedTask == tcs.Task)
            {
                await tcs.Task;
            }
            else
            {
                throw new TimeoutException("The operation has timed out.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Player login request failed: {e.Message} - {e.StackTrace}");
        }
    }

    public static async Task<T> GetVariableCloudAsync<T>(
        string variableName,
        string playerId = "",
        int timeOutMs = 2000
    )
    {
        var tcs = new TaskCompletionSource<T>();
        var timeout = TimeSpan.FromMilliseconds(timeOutMs);

        try
        {
            Debug.Log(
                $"GetVariableCloud request sending with variableName: {variableName} User {playerId}"
            );

            var request = new GetUserDataRequest
            {
                Keys = new List<string> { variableName },
                PlayFabId = playerId,
            };
            PlayFabClientAPI.GetUserData(
                request,
                (result) =>
                {
                    if (result.Data != null && result.Data.ContainsKey(variableName))
                    {
                        var jsonStr = result.Data[variableName].Value;
                        var obj = JsonConvert.DeserializeObject<T>(jsonStr);
                        tcs.SetResult(obj);
                    }
                    else
                    {
                        tcs.SetResult(default(T));
                    }
                },
                (error) =>
                {
                    Debug.LogError($"Failed to get user data: {error.GenerateErrorReport()}");
                    tcs.SetException(
                        new Exception($"Failed to get user data: {error.GenerateErrorReport()}")
                    );
                }
            );

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            else
            {
                throw new TimeoutException("The operation has timed out.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"GetVariableCloudAsync failed: {e.Message} - {e.StackTrace}");
            throw;
        }
    }

    public static async Task<bool> IsExistVariableCloudAsync(
        string variableName,
        int timeOutMs = 2000
    )
    {
        var tcs = new TaskCompletionSource<bool>();
        var timeout = TimeSpan.FromMilliseconds(timeOutMs);

        try
        {
            Debug.Log(
                $"IsExistVariableCloudAsync request sending with variableName: {variableName}"
            );

            var request = new GetUserDataRequest { Keys = new List<string> { variableName } };

            PlayFabClientAPI.GetUserData(
                request,
                (result) =>
                {
                    tcs.SetResult(result.Data != null && result.Data.ContainsKey(variableName));
                },
                (error) =>
                {
                    Debug.LogError($"Failed to check user data: {error.GenerateErrorReport()}");
                    tcs.SetException(
                        new Exception($"Failed to check user data: {error.GenerateErrorReport()}")
                    );
                }
            );

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            else
            {
                throw new TimeoutException("The operation has timed out.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"IsExistVariableCloudAsync failed: {e.Message} - {e.StackTrace}");
            throw;
        }
    }
}
