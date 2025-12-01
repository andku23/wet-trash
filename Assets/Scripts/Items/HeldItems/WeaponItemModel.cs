using UnityEngine;

public class WeaponItemModel : HeldItemModel
{
    public override void UseItem(InteractionController interactionController)
    {
        interactionController.DoAttack();
    }
}
