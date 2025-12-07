using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class RogueEffectManager : NetworkBehaviour
{
    public static RogueEffectManager Instance;

    public GameObject rogueCardPrefab;
    public GameObject voteMarkPrefab;
    public RogueCardsEffects RogueCardsEffects;
    
    private Dictionary<RogueCardEffectID, RogueCardEffectData> _rogueCardsEffectsLookup;
    public Dictionary<RogueCardEffectID, RogueCardEffectData> RogueCardsEffectsLookup {get => _rogueCardsEffectsLookup;}

    private Dictionary<ulong, int> playerCardVote_s;
    private List<GameObject> voteMarks_c = new List<GameObject>();
    private List<RogueCard> rogueCards_c = new List<RogueCard>();
    private void Start()
    {
        Instance = this;
        CreateLookupTables();
    }

    private void CreateLookupTables()
    {
        _rogueCardsEffectsLookup = new Dictionary<RogueCardEffectID, RogueCardEffectData>();
        foreach (RogueCardEffectData data in RogueCardsEffects.PositiveEffects)
        {
            _rogueCardsEffectsLookup.Add(data.id, data);
        }
        
        foreach (RogueCardEffectData data in RogueCardsEffects.NegativeEffects)
        {
            _rogueCardsEffectsLookup.Add(data.id, data);
        }
    }

    public RogueCardEffectData GetRandomPositiveEffect()
    {
        return RogueCardsEffects.PositiveEffects[Random.Range(0, RogueCardsEffects.PositiveEffects.Count)];
    }
    
    public RogueCardEffectData GetRandomNegativeEffect()
    {
        return RogueCardsEffects.NegativeEffects[Random.Range(0, RogueCardsEffects.NegativeEffects.Count)];
    }

    public async Awaitable WaitForCardVote_S(RogueCardPacketData[] cardDatas, Action onComplete)
    {
        float checkInterval = 0.1f;
        playerCardVote_s = new Dictionary<ulong, int>();
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        foreach (var client in connectedClients)
        {
            playerCardVote_s.Add(client.Key, -1);
        }
        
        bool allPlayersResponded = false;
        while (!allPlayersResponded)
        {
            await Awaitable.WaitForSecondsAsync(checkInterval);
            allPlayersResponded = true;
            foreach (var player in playerCardVote_s)
            {
                if (player.Value == -1)
                {
                    allPlayersResponded = false;
                    break;
                }
            }

            string debugWaitMessage = "Vote Status: [";
            foreach (var player in playerCardVote_s)
            {
                debugWaitMessage += $"{player.Key}: {player.Value}, ";
            }
            debugWaitMessage += "]";
            
            Debug.Log(debugWaitMessage);
        }
        
        Debug.Log("a");
        int[] tally = new int[playerCardVote_s.Count];
        // Calculate vote
        foreach (var player in playerCardVote_s)
        {
            tally[player.Value]++;
        }
        
        Debug.Log("b");

        int winner = 0;
        for (int i = 0; i < tally.Length; i++)
        {
            if (tally[winner] <= tally[i])
            {
                winner = i;
            }
        }
        
        Debug.Log($"Winner: Card {winner}");
        ApplyEffects(cardDatas[winner].EffectIDs);
        
        onComplete?.Invoke();
    }

    private void ApplyEffects(RogueCardEffectID[] effectIDs)
    {
        for (int i = 0; i < effectIDs.Length; i++)
        {
            GameObject go = Instantiate(RogueCardsEffectsLookup[effectIDs[i]].prefab);
            BaseRogueEffect effect = go.GetComponent<BaseRogueEffect>();
            effect.ApplyEffect();
            Destroy(go);
        }
    }

    public RogueCardPacketData[] GenerateCards_S(int numPositiveEffects, int numNegativeEffects, int numCards)
    {
        RogueCardPacketData[] datas = new RogueCardPacketData[numCards];
        for (int i = 0; i < numCards; i++)
        {
            datas[i] = GenerateCard_S(numPositiveEffects, numNegativeEffects);
        }

        return datas;
    }
    
    public RogueCardPacketData GenerateCard_S(int numPositiveEffects, int numNegativeEffects)
    {
        RogueCardPacketData loadData = new RogueCardPacketData();
        loadData.EffectIDs = new RogueCardEffectID[numPositiveEffects + numNegativeEffects];
        loadData.description = ""; //TODO populate this
        int counter = 0;
        
        for (int i = 0; i < numPositiveEffects; i++)
        {
            loadData.EffectIDs[counter] = GetRandomPositiveEffect().id;
            counter++;
        }
        
        for (int i = 0; i < numNegativeEffects; i++)
        {
            loadData.EffectIDs[counter] = GetRandomNegativeEffect().id;
            counter++;
        }
        return loadData;
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void ShowCards_ClientRpc(RogueCardPacketData[] loadDatas)
    {
        foreach (RogueCard pastRogueCard in rogueCards_c)
        {
            Destroy(pastRogueCard.gameObject);
        }
        rogueCards_c.Clear();
        for (int i = 0; i < loadDatas.Length; i++)
        {
            RogueCard rogueCard = Instantiate(rogueCardPrefab).GetComponent<RogueCard>();
            rogueCard.LoadCard(loadDatas[i]);
            int cardIndex = i;
            rogueCard.Button.onClick.AddListener(() => {CardVote_ServerRpc(NetworkManager.LocalClientId, cardIndex);});
            rogueCards_c.Add(rogueCard);
        }
        GameUI.Instance.ShowRogueCards(rogueCards_c);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CardVote_ServerRpc(ulong playerID,int cardIndex)
    {
        playerCardVote_s[playerID] = cardIndex;
        
        ulong[] players = playerCardVote_s.Keys.ToArray();
        int[] votes = playerCardVote_s.Values.ToArray();
        
        UpdateVotingState_ClientRpc(players, votes);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void UpdateVotingState_ClientRpc(ulong[] players, int[] votes)
    {
        foreach (GameObject go in voteMarks_c)
        {
            Destroy(go);
        }
        voteMarks_c.Clear();
        for (int i = 0; i < players.Length; i++)
        {
            if (votes[i] > -1)
            {
                GameObject voterMark = Instantiate(voteMarkPrefab, rogueCards_c[votes[i]].VoteMarkArea, true);
                voteMarks_c.Add(voterMark);
            }
        }
    }
    
    #region Rogue Effects
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeQuota_ServerRpc(float newQuota)
    {
        ChangeQuota_ClientRpc(newQuota);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void ChangeQuota_ClientRpc(float newQuota)
    {
        GameManager.Instance.gameData.QUOTA_PERCENTAGE = newQuota;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeWeightMultiplier_ServerRpc(float target)
    {
        ChangeWeightMultiplier_ClientRpc(target);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void ChangeWeightMultiplier_ClientRpc(float target)
    {
        GameManager.Instance.gameData.WEIGHT_MULTIPLIER = target;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeMonsterSpawnRate_ServerRpc(int target)
    {
        ChangeMonsterSpawnRate_ClientRpc(target);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void ChangeMonsterSpawnRate_ClientRpc(int target)
    {
        GameManager.Instance.gameData.MONSTER_SPAWN_PER_HOUR = target;
    }
    
    #endregion
}
