using UnityEngine;

public interface IDamagable
{
    public int Health { get; }

    public void DoDamage(ItemInteractionData interactionData);
}



[System.Serializable]
public struct ItemInteractionData
{
    public ItemInteractionType interactionType;
    public int damage;
    public int range;
    public ulong networkPlayerID;
}

public enum ItemInteractionType
{
    Mining,
    Attacking
}