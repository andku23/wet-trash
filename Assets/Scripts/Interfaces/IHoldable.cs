using UnityEngine;

public interface IHoldable
{
    public HeldObjectType HeldObjectType { get; }
    // Reference to whatever object it calls back to (like used mostly for cranes)
    public GameObject ConnectedParent { get; set; }
    public GameObject gameObject { get ; }
    public Vector3 HoldAttachOffset();
    public ulong HeldPlayerID { get; set; }
    // Only gets called if held object type is UsableItem
    public void OnEquip(InteractionController interactionController, PlayerState playerState){}
    public void UseItem(InteractionController interactionController, PlayerState playerState){}
    public void OnUnequip(InteractionController interactionController, PlayerState playerState){}
    
}

public enum HeldObjectType
{
    None = 0,
    Inventorable = 1, //I should one day change this to check if IInventorable type on holdable
    TemporaryHold = 2,
    InventorableUseable = 3,
}
