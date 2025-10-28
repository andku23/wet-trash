using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance;
    
    private NetworkVariable<int> _cash = new NetworkVariable<int>();
    public UnityEvent<int> OnCashChanged = new UnityEvent<int>();
    private int _currentDayCash = 0;
    
    public int Cash {get{return _cash.Value;}}
    public int CurrentDayCash {get{return _currentDayCash;}set{_currentDayCash=value;}}
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }
        
        _cash.OnValueChanged += OnCashUpdated;

        if (IsServer)
        {
            _cash.Value = 400;
        }
        
        OnCashChanged.Invoke(_cash.Value);

    }

    public void AddCash(int addAmount)
    {
        AddCash_ServerRpc(addAmount);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void AddCash_ServerRpc(int addAmount)
    {
        _cash.Value += addAmount;
        CurrentDayCash += addAmount;
    }
    
    public void SubtractCash(int subAmount)
    {
        AddCash(-subAmount);
    }
    
    public void ResetCurrentCollected()
    {
        CurrentDayCash = 0;
    }
    
    public void OnCashUpdated(int prev, int next)
    {
        OnCashChanged.Invoke(next);
    }
}
