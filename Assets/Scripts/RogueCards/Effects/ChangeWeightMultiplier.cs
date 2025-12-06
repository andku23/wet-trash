using UnityEngine;

public class ChangeWeightMultiplier : BaseRogueEffect
{
    [SerializeField] private float weightMultiplier;
    
    public override void ApplyEffect()
    {
        RogueEffectManager.Instance.ChangeWeightMultiplier_ServerRpc(weightMultiplier);
    }
}
