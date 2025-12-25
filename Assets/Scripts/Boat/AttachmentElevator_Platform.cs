using UnityEngine;

public class AttachmentElevator_Platform : MonoBehaviour, IInteractable
{
    [SerializeField] private AttachmentElevator main;
    [SerializeField] private WorldspaceInstruction worldspaceInstruction;

    public void AssignMain(AttachmentElevator elevator)
    {
        main = elevator;
    }
    
    public void Interact(ulong networkPlayerID, InteractionButtonType buttonType)
    {
        if (buttonType == InteractionButtonType.Interact)
        {
            //TODO Implement interaction to match Loot Deposit
            //main.PlaceLoot_ServerRpc(networkPlayerID, );
        }
    }

    public void InteractHeld(ulong networkPlayerID, InteractionButtonType buttonType, IHoldable heldObject)
    {
        if (buttonType == InteractionButtonType.Interact)
        {
            LootManager.Instance.RequestDrop(transform.position, heldObject.gameObject, true, main.NetworkObjectId);
        }
    }

    public bool EnableInteractable(IHoldable heldObject)
    {
        if (heldObject == null) return false;
        worldspaceInstruction.SetText("'E' to place");
        worldspaceInstruction.SetVisible(true);
        return true;
    }
    
    public void DisableInteractable()
    {
        worldspaceInstruction.SetVisible(false);
    }
}
