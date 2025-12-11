using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance;
    
    //private NetworkVariable<int> _cash = new NetworkVariable<int>();
    private NetworkList<int> _wallet = new NetworkList<int>();
    
    public UnityEvent<CurrencyType, int> OnCashChanged = new UnityEvent<CurrencyType, int>();
    //private int _currentDayCash = 0;
    public List<int> CurrentDayCash_S = new List<int>();
    
    public int Cash {get{return 0;}}
    public NetworkList<int> Wallet {get{return _wallet;}}

    public int CurrentDayCash
    {
        get
        {
            int total = 0;
            foreach (var item in CurrentDayCash_S)
            {
                total += item;
            }

            return total;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }
        
        
        //_cash.OnValueChanged += OnCashUpdated;
        _wallet.OnListChanged += OnCashUpdated;

        if (IsServer)
        {
            int[] generatedWallet = VarietyUtilities.GetInitializedCurrencyArray();
            foreach (int i in generatedWallet)
            {
                _wallet.Add(0);
                CurrentDayCash_S.Add(0);
            }

            //_cash.Value = GameManager.Instance.gameData.INITIAL_CASH;
        }

        for (int i = 0; i < _wallet.Count; i++)
        {
            OnCashChanged.Invoke((CurrencyType) i, _wallet[i]);
        }

    }

    public void CashInLoot(int itemDataIndex)
    {
        CashInLoot_ServerRpc(itemDataIndex);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void CashInLoot_ServerRpc(int itemDataIndex)
    {
        var itemData = LootManager.Instance.LootIndextoData(itemDataIndex);

        foreach (var currency in itemData.valueRange)
        {
            _wallet[(int)currency.CurrencyType] += currency.MaxValue;
            CurrentDayCash_S[(int)currency.CurrencyType] += currency.MaxValue;
        }
    }
    
    public void SubtractCash(CurrencyType currencyType, int subAmount)
    {
        ChangeCash_ServerRpc(currencyType, -subAmount);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeCash_ServerRpc(CurrencyType currencyType, int amount)
    {
        _wallet[(int)currencyType] += amount;
        CurrentDayCash_S[(int)currencyType] += amount;
    }
    
    public void ResetCurrentCollected_S()
    {
        for (int i = 0; i < CurrentDayCash_S.Count; i++)
        {
            CurrentDayCash_S[i] = 0;
        }
    }
    
    public void OnCashUpdated(NetworkListEvent<int> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<int>.EventType.Value)
        {
            OnCashChanged.Invoke((CurrencyType)changeEvent.Index, changeEvent.Value);
        }
    }
}
