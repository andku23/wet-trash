using UnityEngine;

public interface IHoldable
{
    public HeldObjectType HeldObjectType { get; }
    public GameObject ConnectedParent { get; set; }
    public GameObject gameObject { get ; } 
}

public enum HeldObjectType
{
    Default = 0,
    Loot = 1,
    CraneHook = 2
}
