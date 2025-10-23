using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance;
    
    private NetworkVariable<int> _cash = new NetworkVariable<int>();
    public UnityEvent<int> OnCashChanged = new UnityEvent<int>();
    
    public int Cash {get{return _cash.Value;}}
    
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
    }
    
    public void SubtractCash(int subAmount)
    {
        AddCash(-subAmount);
    }
    
    public void OnCashUpdated(int prev, int next)
    {
        OnCashChanged.Invoke(next);
    }
}
