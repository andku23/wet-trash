using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "RogueCardsEffects", menuName = "Scriptable Objects/RogueCardsEffects")]
public class RogueCardsEffects : ScriptableObject
{
    public List<RogueCardEffectData> PositiveEffects;
    public List<RogueCardEffectData> NegativeEffects;
}

[System.Serializable]
public class RogueCardEffectData
{
    public RogueCardEffectID id;
    public string title;
    public string description;
    public GameObject prefab;
}

public enum RogueCardEffectID
{
    QuotaPercentUp,
    QuotaPercentDown,
    LootWeightUp,
    LootWeightDown,
    MonsterRateUp,
    MonsterRateDown,
    BreathUp,
    BreathDown,
    MonsterDetectionUp,
    MonsterDetectionDown,
}
