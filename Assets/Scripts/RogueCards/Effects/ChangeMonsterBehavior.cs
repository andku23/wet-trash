using Unity.Netcode;
using UnityEngine;

public class ChangeMonsterBehavior : BaseRogueEffect
{
    [SerializeField] private float damageMultiplier;
    [SerializeField] private float detectionMultiplier;
    
    public override void ApplyEffect()
    {
        GameData current = GameManager.Instance.gameData;
        RogueEffect_MonsterBehaviorPacket data = new RogueEffect_MonsterBehaviorPacket();
        data.DamageMultiplier = current.MONSTER_DAMAGE_MULTIPLIER * damageMultiplier;
        data.DetectionMultiplier = current.MONSTER_DETECTION_RANGE_MULTIPLIER * detectionMultiplier;
        RogueEffectManager.Instance.ChangeMonsterBehavior_ServerRpc(data);
    }
}

public struct RogueEffect_MonsterBehaviorPacket : INetworkSerializable
{
    public float DamageMultiplier;
    public float DetectionMultiplier;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref DamageMultiplier);
        serializer.SerializeValue(ref DetectionMultiplier);
    }
}