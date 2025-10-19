using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerDeath : NetworkBehaviour
{
    private int _animIDIsDead;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private Animator _animator;
    
    private float _breath;
    private StarterAssetsInputs _input;
    
    public override void OnNetworkSpawn()
    {
        _animIDIsDead = Animator.StringToHash("IsDead");
        _breath = playerState.BreathFullAmount;
        
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
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
        _animator.SetBool(_animIDIsDead, true);
        playerState.IsDead = true;
    }
    
    // Client Rpc Callback
    public void RevivePlayerLocal()
    {
        _breath = playerState.BreathFullAmount;
        _animator.SetBool(_animIDIsDead, false);
        playerState.IsDead = false;
    }
}
