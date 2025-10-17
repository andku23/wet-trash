using Unity.Netcode;
using UnityEngine;

public class NetworkHandleParenting : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        
    }

    public void RequestParentTo(ulong networkPlayerID, ulong transformNetworkID)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(transformNetworkID, out var netObj);
        NetworkManager.ConnectedClients[networkPlayerID].PlayerObject.transform.SetParent(netObj.transform, true);
        RequestParentTo_ServerRPC(networkPlayerID, transformNetworkID);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestParentTo_ServerRPC(ulong networkPlayerID, ulong transformNetworkID)
    {
        RequestParentTo_ClientRPC(networkPlayerID, transformNetworkID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void RequestParentTo_ClientRPC(ulong networkPlayerID, ulong transformNetworkID)
    {
        Debug.Log("Request to parent");
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(transformNetworkID, out var netObj);
        NetworkManager.ConnectedClients[networkPlayerID].PlayerObject.transform.SetParent(netObj.transform, true);
    }
    
    public void RequestUnparentTo(ulong networkPlayerID)
    {
        NetworkManager.ConnectedClients[networkPlayerID].PlayerObject.transform.SetParent(null, true);
        RequestUnparentTo_ServerRPC(networkPlayerID);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestUnparentTo_ServerRPC(ulong networkPlayerID)
    {
        RequestUnparentTo_ClientRPC(networkPlayerID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void RequestUnparentTo_ClientRPC(ulong networkPlayerID)
    {
        Debug.Log("Request to Unparent");
        NetworkManager.ConnectedClients[networkPlayerID].PlayerObject.transform.SetParent(null, true);
    }
}
