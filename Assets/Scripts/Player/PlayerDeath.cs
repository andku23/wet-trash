using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PlayerDeath : NetworkBehaviour
{
    private int _animIDIsDead;
    [SerializeField] private PlayerState playerState;
    [FormerlySerializedAs("_animator")] public Animator Animator;
    
    private float _breath;
    private StarterAssetsInputs _input;
    
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
        if (next <= 0)
        {
            GameManager.Instance.RequestPlayerDeath();
        }
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
            GameManager.Instance.RequestPlayerDeath();
        } else if (playerState.IsDead)
        {
            if (_input.respawn)
            {
                GameManager.Instance.RequestPlayerRevive();
                _input.respawn = false;
            }
        } 
    }
    
    // Client Rpc Callback
    public void KillPlayerLocal()
    {
        Animator.SetBool(_animIDIsDead, true);
        playerState.IsDead = true;
    }
    
    // Client Rpc Callback
    public void RevivePlayerLocal()
    {
        _breath = playerState.BreathFullAmount;
        playerState.Health.Value = playerState.MAX_HEALTH;
        Animator.SetBool(_animIDIsDead, false);
        playerState.IsDead = false;
    }
}
