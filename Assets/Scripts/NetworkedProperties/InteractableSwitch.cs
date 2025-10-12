using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class InteractableSwitch : MonoBehaviour, IInteractable
{

    [SerializeField] private GameObject instructions;
    [SerializeField] private bool isToggleButton;
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent<bool> onToggled;
    [SerializeField] private Animator animator;
    
    private ClientStateMachine _stateMachine;
    private bool _isToggled;
    
    // animation IDs
    private int _animIDPress;
    private int _animIDIsToggled;

    enum States
    {
        Default = 0,
        ClosestItem = 1
    };
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, OnDefaultStateUpdate, OnDefaultStateExit);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, OnClosestItemStateUpdate, OnClosestItemStateExit);
        _stateMachine.AddState((int)States.ClosestItem, closestItemState);
        
        _stateMachine.ChangeState((int)States.Default);
        
        _animIDPress = Animator.StringToHash("Press");
        _animIDIsToggled = Animator.StringToHash("IsToggled");
    }

    public void Interact()
    {
        if (isToggleButton)
        {
            _isToggled = !_isToggled;
            onToggled?.Invoke(_isToggled);
            animator.SetBool(_animIDIsToggled, _isToggled);
        }
        else
        {
            onPressed?.Invoke();
            animator.SetTrigger(_animIDPress);
        }
    }
    
    #region States

    private void OnDefaultStateEnter()
    {
        instructions.SetActive(false);
    }
    
    private void OnDefaultStateUpdate()
    {
        
    }
    
    private void OnDefaultStateExit()
    {
        
    }
    
    private void OnClosestItemStateEnter()
    {
        instructions.SetActive(true);
    }
    
    private void OnClosestItemStateUpdate()
    {
    }
    
    private void OnClosestItemStateExit()
    {
        instructions.SetActive(false);
    }
    
    #endregion

    public void SetAsInteractable(bool isInteractable)
    {
        if (isInteractable)
        {
            _stateMachine.ChangeState((int)States.ClosestItem);
        }
        else
        {
            _stateMachine.ChangeState((int)States.Default);
        }
    }
}
