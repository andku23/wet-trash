using UnityEngine;

public abstract class HeldItemModel : MonoBehaviour
{
    public GameObject CraneAttachPoint;

    public GameObject HoldAttachPoint;

    public virtual void OnEquip(InteractionController interactionController, PlayerState playerState)
    {
        
    }

    public virtual void UseItem(InteractionController interactionController, PlayerState playerState)
    {
        
    }
    
    public virtual void OnUnequip(InteractionController interactionController, PlayerState playerState)
    {
        
    }
}
