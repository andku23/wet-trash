using UnityEngine;

public interface IHoldable
{
    public HeldObjectType HeldObjectType { get; }
    // Reference to whatever object it calls back to (like used mostly for cranes)
    public GameObject ConnectedParent { get; set; }
    public GameObject gameObject { get ; }
    public ulong HeldPlayerID { get; set; }

    public float GetWeightMultiplier()
    {
        return 1.0f;
    }
}

public enum HeldObjectType
{
    Default = 0,
    Loot = 1,
    CraneHook = 2,
    SteeringWheel = 3
}
