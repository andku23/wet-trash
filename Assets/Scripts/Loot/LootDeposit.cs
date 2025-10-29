using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LootDeposit : NetworkBehaviour, IInteractable
{
    [SerializeField] private GameObject instructions;
    [SerializeField] public NetworkVariable<int> id;
    [SerializeField] private GameObject lootDepositBox;
    [SerializeField] private Collider collider;

    [SerializeField] private int rows = 3;
    [SerializeField] private int columns = 3;

    private ClientStateMachine _stateMachine;
    private List<GameObject> _lootDepositsBoxes;

    enum States
    {
        Default = 0,
        ClosestItem = 1
    };
    
    public int Size {get; set;}
    
    public override void OnNetworkSpawn ()
    {
        _stateMachine = new ClientStateMachine();
        
        BaseState defaultState = new BaseState(OnDefaultStateEnter, OnDefaultStateUpdate, OnDefaultStateExit);
        _stateMachine.AddState((int)States.Default, defaultState);
        
        BaseState closestItemState = new BaseState(OnClosestItemStateEnter, OnClosestItemStateUpdate, OnClosestItemStateExit);
        _stateMachine.AddState((int)States.ClosestItem, closestItemState);
        
        _stateMachine.ChangeState((int)States.Default);

        if (IsServer)
        {
            LootManager.Instance.RegisterDepositServer(this);
        }
    }

    public void PlaceLootAtNextPosition()
    {
        Size++;
        
        GameObject go = Instantiate(lootDepositBox, transform);
        int indexedSize = Size - 1;
        Vector3 t = new Vector3();
        
        // float xStart = collider.bounds.min.x;
        // float yStart = collider.bounds.min.y;
        // float xSpacing = (collider.bounds.max.x - collider.bounds.min.x)/rows;
        // float ySpacing = (collider.bounds.max.y - collider.bounds.min.y)/columns;
        
        
        float xSpacing = (go.transform.localScale.x)/rows;
        float ySpacing = (go.transform.localScale.y)/rows;
        float zSpacing = (go.transform.localScale.z)/columns;
        
        float xStart = -go.transform.localScale.x/2 + xSpacing/2;
        float yStart = 0.0f;
        float zStart = -go.transform.localScale.z/2 + zSpacing/2;

        int height = Mathf.FloorToInt(indexedSize / (rows * columns));
        int column = Mathf.FloorToInt(indexedSize % (rows * columns) / columns);
        int row = Mathf.FloorToInt(indexedSize % (rows * columns) % columns);
        
        //Debug.Log("Row: " + row + " Column: " + column + "Height: " + height);
        
        t.x = xStart + (row * xSpacing);
        t.y = yStart + (height * ySpacing);
        t.z = zStart + (column * zSpacing);

        
        go.transform.localPosition = t;
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        if (heldObject == null) return false;
        _stateMachine.ChangeState((int)States.ClosestItem);
        return true;
    }
    
    public void DisableInteractable()
    {
        _stateMachine.ChangeState((int)States.Default);
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
