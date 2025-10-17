using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance;
    
    private NetworkVariable<int> _cash = new NetworkVariable<int>(0);
    public UnityEvent<int> OnCashChanged = new UnityEvent<int>();
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }

        _cash.OnValueChanged += OnCashUpdated;
    }

    public void AddCash(int addAmount)
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
