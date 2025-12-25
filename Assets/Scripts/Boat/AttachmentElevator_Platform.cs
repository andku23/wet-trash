using UnityEngine;

public class AttachmentElevator_Platform : MonoBehaviour, IInteractable
{
    [SerializeField] private AttachmentElevator main;
    [SerializeField] private WorldspaceInstruction worldspaceInstruction;
    
    public void Interact(ulong networkPlayerID, InteractionButtonType buttonType)
    {
        if (buttonType == InteractionButtonType.Interact)
        {
            //TODO Implement interaction to match Loot Deposit
            //main.PlaceLoot_ServerRpc(networkPlayerID, );
        }
    }

    public bool EnableInteractable(IHoldable heldObject)
    {
        worldspaceInstruction.SetText("'E' to place");
        worldspaceInstruction.SetVisible(true);
        return true;
    }
    
    public void DisableInteractable()
    {
        worldspaceInstruction.SetVisible(false);
    }
}
