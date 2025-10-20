using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private LootManager _lootManager;
    [SerializeField] private UI _ui;
    [SerializeField] private float _timeFullDaySeconds;
    [SerializeField] private UnityEvent<int> _timeUpdatedEvent;
    [SerializeField] private UnityEvent _timeFinishedEvent;
    [SerializeField] private UnityEvent<int> _onDayUpdatedEvent;
    [SerializeField] public UnityEvent<float> OnBreathUpdated;
    
    private TimeState _timeState;
    private Coroutine _co_TimerCountdown;
    private int _day = 0;
    
    public enum TimeState
    {
        None = 0,
        DayActive = 1,
        BetweenDays = 2
    }
    
    public static GameManager Instance;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void RequestToggleDay()
    {
        switch (_timeState)
        {
            case TimeState.DayActive:
                ToggleBeginDay_ServerRpc(false);
                break;
            case TimeState.BetweenDays:
            case TimeState.None:
                ToggleBeginDay_ServerRpc(true);
                break;
        }
    }

    public void RequestStartDay()
    {
        ToggleBeginDay_ServerRpc(true);
    }
    
    public void RequestEndDay()
    {
        ToggleBeginDay_ServerRpc(false);
    }

    #region Player Death
    public void RequestPlayerDeath()
    {
        PlayerDeath_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerDeath_ServerRpc(ulong targetPlayerNetworkObjectId)
    {
        PlayerDeath_ClientRpc(targetPlayerNetworkObjectId);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void PlayerDeath_ClientRpc(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        pickupPlayerClient.PlayerObject.GetComponent<PlayerDeath>().KillPlayerLocal();
    }
    
    public void RequestPlayerRevive()
    {
        PlayerRevive_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerRevive_ServerRpc(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        pickupPlayerClient.PlayerObject.transform.position = Vector3.zero;
        PlayerRevive_ClientRpc(targetPlayerNetworkObjectId);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void PlayerRevive_ClientRpc(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        pickupPlayerClient.PlayerObject.GetComponent<PlayerDeath>().RevivePlayerLocal();
    }
    #endregion
    
    [ServerRpc(RequireOwnership = false)]
    private void ToggleBeginDay_ServerRpc(bool startDay)
    {
        if (startDay && _timeState == TimeState.DayActive) return;
        if (!startDay && _timeState == TimeState.BetweenDays) return;
        _timeState = startDay ? TimeState.DayActive : TimeState.BetweenDays;
        
        if (_timeState == TimeState.DayActive)
        {
            _day++;
            SpawnLoot();
        }
        else if (_timeState == TimeState.BetweenDays)
        {
            DeleteLoot();
        }
        ToggleBeginDay_ClientRpc(_timeState, _day);
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void ToggleBeginDay_ClientRpc(TimeState timeState, int day)
    {
        _timeState = timeState;
        _day = day;
        if (_timeState == TimeState.DayActive)
        {
            StartCountdown();
        }
        else if (_timeState == TimeState.BetweenDays)
        {
            StopCountdown();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnLoot_ServerRpc()
    {
        SpawnLoot();
    }


    private void SpawnLoot()
    {
        DeleteLoot();
        _lootManager.SpawnLoot();
    }
    
    private void DeleteLoot()
    {
        _lootManager.DeleteAllLoot();
    }
    
    private void StartCountdown()
    {
        StopCountdown();
        _onDayUpdatedEvent.Invoke(_day);
        _co_TimerCountdown = StartCoroutine(Co_TimerCountdown());
    }
    
    private void StopCountdown()
    {
        _timeUpdatedEvent?.Invoke(0);
        if(_co_TimerCountdown != null) StopCoroutine(_co_TimerCountdown);
    }
    
    private IEnumerator Co_TimerCountdown()
    {
        int secondsRemaining = Mathf.FloorToInt(_timeFullDaySeconds);
        while (secondsRemaining > 0)
        {
            _timeUpdatedEvent?.Invoke(secondsRemaining);
            yield return new WaitForSeconds(1);
            secondsRemaining--;
        }
        _timeFinishedEvent?.Invoke();
    }
}
