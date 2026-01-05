using Unity.Netcode;
using UnityEngine;

public class CaveDoor : NetworkBehaviour, IInteractable
{
    public NetworkVariable<int> DestinationDoorID = new NetworkVariable<int>();

    [SerializeField] private WorldspaceInstruction instruction;
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        if (heldObject != null) return false;
        instruction.SetVisible(true);
        return true;
    }

    public void Interact(ulong networkPlayerID, InteractionButtonType buttonType)
    {
        if (buttonType == InteractionButtonType.Interact)
        {
            Debug.Log("going to door: " + DestinationDoorID.Value);
            ulong destinationDoorNetworkID = DungeonManager.Instance.spawnedDoors[DestinationDoorID.Value];
            NetworkObject no = NetworkManager.Singleton.SpawnManager.SpawnedObjects[destinationDoorNetworkID];
            CaveDoor destinationDoor = no.GetComponent<CaveDoor>();

            if (destinationDoor != null)
            {
                NetworkManager.Singleton.LocalClient.PlayerObject.transform.position = destinationDoor.transform.position;
            }
        }
    }

    public void DisableInteractable()
    {
        instruction.SetVisible(false);
    }
}
