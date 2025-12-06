using UnityEngine;

public class ChangeQuotaEffect : BaseRogueEffect
{
    [SerializeField] private float quotaPercentage;
    
    public override void ApplyEffect()
    {
        RogueEffectManager.Instance.ChangeQuota_ServerRpc(quotaPercentage);
    }
}
