using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance;
    
    //private NetworkVariable<int> _cash = new NetworkVariable<int>();
    private NetworkList<int> _wallet;
    
    public UnityEvent<CurrencyType, int> OnCashChanged = new();
    //private int _currentDayCash = 0;
    [HideInInspector] public List<int> CurrentDayCash_C = new();
    
    public int Cash {get{return 1000;}}
    public NetworkList<int> Wallet => _wallet;

    public int CurrentAllLootTotal_S
    {
        get
        {
            int total = 0;
            foreach (var item in CurrentDayCash_C)
            {
                total += item;
            }

            return total;
        }
    }

    private void Awake()
    {
        _wallet = new NetworkList<int>();
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
                CurrentDayCash_C.Add(0);
            }

            foreach (var currencyValuePair in GameManager.Instance.gameData.INITIAL_CASH)
            {
                _wallet[(int)currencyValuePair.CurrencyType] += currencyValuePair.Value;
            }
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
            CurrentDayCash_C[(int)currency.CurrencyType] += currency.MaxValue;
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
        CurrentDayCash_C[(int)currencyType] += amount;
    }
    
    public void ResetCurrentCollected_C()
    {
        for (int i = 0; i < CurrentDayCash_C.Count; i++)
        {
            CurrentDayCash_C[i] = 0;
        }
    }
    
    public void OnCashUpdated(NetworkListEvent<int> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<int>.EventType.Value)
        {
            OnCashChanged.Invoke((CurrencyType)changeEvent.Index, changeEvent.Value);
            CurrentDayCash_C[changeEvent.Index] += changeEvent.Value;
        }
    }
}
