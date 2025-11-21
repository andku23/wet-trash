using UnityEngine;

public class WeaponItemModel : HeldItemModel
{
    public override void UseItem(PlayerController player)
    {
        player.DoAttack();
    }
}
