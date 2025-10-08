using UnityEngine;
using Unity.Netcode;

public class RevealIfOwner : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            Destroy(gameObject);
        }
    }
}
