using UnityEngine;

public class WeaponItemModel : HeldItemModel
{
    [SerializeField] protected ItemInteractionData interactionData;
    
    public override void UseItem(InteractionController interactionController, PlayerState playerState)
    {
        interactionController.UseHeldItem(interactionData);
    }
}
