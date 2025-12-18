using Unity.Netcode;
using UnityEngine;

public class WeaponItemModel : HeldItemModel
{
    [SerializeField] protected ItemInteractionData interactionData;
    
    public override void UseItem(InteractionController interactionController, PlayerStateData playerState)
    {
        interactionData.networkPlayerID = NetworkManager.Singleton.LocalClientId;
        interactionController.UseHeldItem(interactionData);
    }
}
