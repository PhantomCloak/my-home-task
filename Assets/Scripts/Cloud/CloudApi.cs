using System;
using System.Threading.Tasks;
using UnityEngine;

public static class CloudApi
{
    public static async Task SetVariableCloudAsync<T>(string variableName, T obj)
    {
        try
        {
            Debug.Log($"SetVariableCloud request sending with variableName: {variableName}, obj: {obj}");

            //var writeObject = new WriteStorageObject
            //{
            //    Collection = "personal",
            //    Key = variableName,
            //    Value = JsonConvert.SerializeObject(obj),
            //    PermissionRead = 1,
            //    PermissionWrite = 1
            //};

            //await client.WriteStorageObjectsAsync(session, new[] { writeObject });
        }
        catch (Exception e)
        {
            Debug.LogError($"Player login request failed: {e.Message}" + "-" + e.StackTrace);
        }
    }
}
