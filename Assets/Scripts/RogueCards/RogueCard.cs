using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RogueCard : MonoBehaviour
{
    private List<RogueCardEffectData> EffectDatas;

    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    
    public Button Button;

    public void LoadCard(RogueCardPacketData loadData)
    {
        EffectDatas = new List<RogueCardEffectData>();
        for (int i = 0; i < loadData.EffectIDs.Length; i++)
        { 
            EffectDatas.Add(RogueEffectManager.Instance.RogueCardsEffectsLookup[loadData.EffectIDs[i]]);
        }
        
        description.text = "";
        foreach (RogueCardEffectData effectData in EffectDatas)
        {
            description.text += effectData.description + "\n";
        }
    }

    // Should probably move this out of here to manager at some point
    public void ApplyCard_S()
    {
        foreach (RogueCardEffectData effectData in EffectDatas)
        {
            BaseRogueEffect effect = Instantiate(effectData.prefab).GetComponent<BaseRogueEffect>();
            effect.ApplyEffect();
            Destroy(effect.gameObject);
        }
    }
}


public class RogueCardPacketData: INetworkSerializable
{
    public RogueCardEffectID[] EffectIDs;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref EffectIDs);
    }
}
