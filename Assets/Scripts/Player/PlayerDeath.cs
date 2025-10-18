using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerDeath : MonoBehaviour
{
    private int _animIDIsDead;
    [SerializeField] private ThirdPersonController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private float BREATH_FULL_AMOUNT;
    [SerializeField] private float BREATH_DEPLETE_RATE;
    [SerializeField] private float BREATH_REPLENISH_RATE;
    
    
    private float _breath;
    
    private void Start()
    {
        _animIDIsDead = Animator.StringToHash("IsDead");
        
        _breath = BREATH_FULL_AMOUNT;
    }

    private void Update()
    {
        if (_controller.InWater && !_controller.InWaterOnSurface)
        {
            _breath -= Time.deltaTime * BREATH_REPLENISH_RATE;
            _breath = Mathf.Clamp(_breath, 0, BREATH_FULL_AMOUNT);
        }
        else
        {
            _breath += Time.deltaTime * BREATH_DEPLETE_RATE;
            _breath = Mathf.Clamp(_breath, 0, BREATH_FULL_AMOUNT);
        }
        GameManager.Instance.OnBreathUpdated.Invoke(_breath/BREATH_FULL_AMOUNT);
        if (_breath <= 0.0f && !_controller.IsDead)
        {
            
            GameManager.Instance.RequestPlayerDeath();
        } 
    }
    
    // Client Rpc Callback
    public void KillPlayerLocal()
    {
        _animator.SetBool(_animIDIsDead, true);
        _controller.IsDead = true;
    }
    
    // Client Rpc Callback
    public void RevivePlayerLocal()
    {
        _animator.SetBool(_animIDIsDead, false);
        _controller.IsDead = false;
    }
}
