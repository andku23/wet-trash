using UnityEngine;

public interface IHoldable
{
    public HeldObjectType HeldObjectType { get; }
    public GameObject ConnectedParent { get; set; }
    public GameObject gameObject { get ; }

    public float GetWeightMultiplier()
    {
        return 1.0f;
    }
}

public enum HeldObjectType
{
    Default = 0,
    Loot = 1,
    CraneHook = 2
}
