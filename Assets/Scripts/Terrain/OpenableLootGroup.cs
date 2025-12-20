using UnityEngine;

public class OpenableLootGroup : LootGroup, IInteractable
{
    [SerializeField] private Animator _animator;
    [SerializeField] private WorldspaceInstruction _worldspaceInstruction;
    
    public void Interact(ulong networkPlayerID, InteractionButtonType buttonType)
    {
        if (buttonType == InteractionButtonType.Interact)
        {
            _animator.SetTrigger("Open");
        }
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        _worldspaceInstruction.SetVisible(true);
        return true;
    }

    public void DisableInteractable()
    {
        _worldspaceInstruction.SetVisible(false);
    }
    
}
