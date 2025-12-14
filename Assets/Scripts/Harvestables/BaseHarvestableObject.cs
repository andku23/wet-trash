using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseHarvestableObject : NetworkBehaviour, IDamagable
{
    [SerializeField] protected List<int> _lootDroppedOnDeath = new List<int>();
    [SerializeField] protected ItemInteractionType[] interactableTypes;
    
    [SerializeField] protected NetworkVariable<int> _health = new NetworkVariable<int>(0);
    protected NetworkVariable<int> _networkState = new NetworkVariable<int>(0);
    protected ClientStateMachine _stateMachine;
    
    public int Health { get { return _health.Value; } }
    
    public override void OnNetworkSpawn()
    {
        InitializeStateMachine();
        
        if (IsServer)
        {
            InitializeServerValues();
        }

        _networkState.OnValueChanged += OnNetworkStateUpdated;
        _health.OnValueChanged += OnHealthUpdated;
        
        //Sync network state to whatever server is on
        OnNetworkStateUpdated(0, _networkState.Value);
    }
    
    protected virtual void InitializeStateMachine()
    {
        _stateMachine = new ClientStateMachine();
    }
    
    protected virtual void InitializeServerValues()
    {
    }
    
    protected virtual void OnHealthUpdated(int prev, int next)
    {
        if (_health.Value <= 0)
        {
            DoDeath();
        }
    }
    
    protected virtual async Awaitable DoDeath()
    {
        if (IsServer)
        {
            DropLoot_S();
        }
    }
    
    public virtual void DoDamage(ItemInteractionData interactionData)
    {
        if (!ContainsInteractableType(interactionData.interactionType)) return;
        DoDamage_ServerRpc(interactionData.interactionType, interactionData.damage);
    }
    
    protected virtual void OnNetworkStateUpdated(int prev, int next)
    {
        _stateMachine.ChangeState(next);
    }
    
    protected virtual void DropLoot_S()
    {
        foreach (var loot in _lootDroppedOnDeath)
        {
            LootManager.Instance.SpawnAndLoadLoot_S(transform.position, loot);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    protected void ChangeState_ServerRpc(int newState)
    {
        _networkState.Value = newState;
    }
    
    [ServerRpc(RequireOwnership = false)]
    protected void DoDamage_ServerRpc(ItemInteractionType interactionType, int amount)
    {
        _health.Value -= amount;
    }
    
    protected virtual void Update()
    {
        if(_stateMachine != null)
            _stateMachine.Update();
    }

    protected bool ContainsInteractableType(ItemInteractionType interactionType)
    {
        bool hasType = false;
        for (int i = 0; i < interactableTypes.Length; i++)
        {
            if (interactableTypes[i] == interactionType)
            {
                hasType = true;
                break;
            }
        }
        return hasType;
    }
}

