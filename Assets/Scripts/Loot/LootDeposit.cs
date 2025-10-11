using Unity.Netcode;
using UnityEngine;

public class LootDeposit : MonoBehaviour, IInteractable
{
    
    [SerializeField] private GameObject instructions;
    [SerializeField] private int id;

    private ClientStateMachine _stateMachine;

    enum States
    {
        Default = 0,
        ClosestItem = 1
    };
    
    public int ID {get; private set;}
    
    private void Start()
    {
        ID = id;
        
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, OnDefaultStateUpdate, OnDefaultStateExit);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, OnClosestItemStateUpdate, OnClosestItemStateExit);
        _stateMachine.AddState((int)States.ClosestItem, closestItemState);
        
        _stateMachine.ChangeState((int)States.Default);
    }

    public Vector3 GetRandomPosition()
    {
        Vector3 t = new Vector3();
        
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            t.x = Random.Range(collider.bounds.min.x, collider.bounds.max.x);
            t.y = transform.position.y + 0.5f;
            t.z = Random.Range(collider.bounds.min.z, collider.bounds.max.z);
        }
        
        return t;
    }
    
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
    
    
    
}
