using UnityEngine;

public abstract class HeldItemModel : MonoBehaviour
{
    public GameObject CraneAttachPoint;
    public GameObject HoldAttachPoint;

    public virtual void OnEquip(InteractionController interactionController, PlayerStateData playerState)
    {
        
    }

    public virtual void UseItem(InteractionController interactionController, PlayerStateData playerState)
    {
        
    }
    
    public virtual void OnUnequip(InteractionController interactionController, PlayerStateData playerState)
    {
        
    }
}
