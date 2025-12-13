using UnityEngine;

public class WeaponItemModel : HeldItemModel
{
    [SerializeField] protected ItemInteractionType itemInteractionType;
    [SerializeField] protected int damage;
    
    public override void UseItem(InteractionController interactionController, PlayerState playerState)
    {
        interactionController.UseHeldItem(itemInteractionType, damage);
    }
}
