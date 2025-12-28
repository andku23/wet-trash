using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PlayerStateController : NetworkBehaviour
{
    private int _animIDIsDead;
    [SerializeField] private PlayerStateData playerState;
    [SerializeField] private ItemData deadPlayerItem;
    public Animator Animator;
    
    private float _breath;
    private StarterAssetsInputs _input;

    public UnityEvent OnDeath;
    public UnityEvent OnRevive;
    
    public override void OnNetworkSpawn()
    {
        _animIDIsDead = Animator.StringToHash("IsDead");
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];

        if (IsOwner)
        {
            _breath = playerState.BreathFullAmount;
            playerState.Health.OnValueChanged += OnHealthChanged;
        }
    }

    private void OnHealthChanged(float prev, float next)
    {
        if (prev > 0 && next <= 0)
        {
            KillPlayerLocal();
        }
        
        else if (prev <= 0 && next > 0)
        {
            RevivePlayerLocal();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoDamage_ServerRpc(float damage)
    {
        playerState.Health.Value -= damage;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (playerState.InWater && !playerState.InWaterOnSurface)
        {
            _breath -= Time.deltaTime * playerState.BreathDepleteRate;
            _breath = Mathf.Clamp(_breath, 0, playerState.BreathFullAmount);
        }
        else
        {
            _breath += Time.deltaTime * playerState.BreathReplenishRate;
            _breath = Mathf.Clamp(_breath, 0, playerState.BreathFullAmount);
        }

        GameManager.Instance.OnBreathUpdated.Invoke(_breath/playerState.BreathFullAmount);
        if (_breath <= 0.0f && !playerState.IsDead)
        {
            GameManager.Instance.ChangeHealth(NetworkManager.Singleton.LocalClientId, 0f);
        } else if (playerState.IsDead)
        {
            if (_input.respawn)
            {
                GameManager.Instance.ChangeHealth(NetworkManager.Singleton.LocalClientId, playerState.MAX_HEALTH);
                _input.respawn = false;
            }
        } 
    }
    
    // Client Rpc Callback
    public void KillPlayerLocal()
    {
        Animator.SetBool(_animIDIsDead, true);
        playerState.IsDead = true;
        GameUI.Instance.ShowDeadPanel(true);
        OnDeath?.Invoke();
        if (IsOwner)
        {
            LootManager.Instance.RequestSpawnItem(transform.position, LootManager.Instance.LootDataToItemInstanceData(deadPlayerItem));
        }
    }

    public void ChangeBreathByAmount(float amount)
    {
        _breath += amount;
    }
    
    // Client Rpc Callback
    public void RevivePlayerLocal()
    {
        _breath = playerState.BreathFullAmount;
        //playerState.Health.Value = playerState.MAX_HEALTH;
        Animator.SetBool(_animIDIsDead, false);
        playerState.IsDead = false;
        GameUI.Instance.CloseAllPanels(false);
        OnRevive?.Invoke();
    }
}
