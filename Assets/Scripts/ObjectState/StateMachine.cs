using System;
using System.Collections.Generic;

public interface IState
{
    public Action OnEnter { get; set; }
    public Action OnUpdate { get; set; }
    public Action OnExit { get; set; }
}

public class BaseState : IState
{
    public BaseState(){}
    
    public BaseState(Action onEnter, Action onUpdate, Action onExit)
    {
        OnEnter += onEnter;
        OnUpdate += onUpdate;
        OnExit += onExit;
    }
    
    public Action OnEnter { get; set; }
    public Action OnUpdate { get; set; }
    public Action OnExit { get; set; }
}

public class ClientStateMachine
{
    IState currentState;
    private Dictionary<int, IState> _states = new Dictionary<int, IState>();
    
    public void ChangeState(int stateEnumNumber)
    {
        if (currentState != null)
            currentState.OnExit.Invoke();

        currentState = _states[stateEnumNumber];
        currentState.OnEnter.Invoke();
    }

    public void AddState(int stateEnumNumber, IState newState)
    {
        _states.Add(stateEnumNumber, newState);
    }
    
    public void Update()
    {
        if (currentState != null) currentState.OnUpdate.Invoke();
    }
}
