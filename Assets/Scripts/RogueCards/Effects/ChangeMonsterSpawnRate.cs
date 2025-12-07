using UnityEngine;

public class ChangeMonsterSpawnRate : BaseRogueEffect
{
    [SerializeField] private int spawnRate;
    
    public override void ApplyEffect()
    {
        RogueEffectManager.Instance.ChangeMonsterSpawnRate_ServerRpc(spawnRate);
    }
}
