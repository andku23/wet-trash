using System;
using System.Collections;
using System.Collections.Generic;
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

    public NetworkedBoat Boat;
    
    private TimeState _timeState;
    private Coroutine _co_TimerCountdown;
    private int _day = 0;
    private int _quota = 0;
    
    public enum TimeState
    {
        None = 0,
        DayActive = 1,
        ShowDayResult = 2,
        BetweenDays = 3,
        QuotaFailed = 4,
    }
    
    public static GameManager Instance;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void RequestToNextGameState()
    {
        ToNextGameState_ServerRpc();
    }

    public void ChangeToBuildMode(int shopItemIndex)
    {
        InteractionController.Instance.ChangeToBuildMode(shopItemIndex);
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
    
    [ServerRpc(RequireOwnership = false)]
    public void RespawnAllPlayers_ServerRpc()
    {
        PlayerReviveAll_ClientRpc();
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
    public void PlayerReviveAll_ClientRpc()
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        foreach (var connectedClient in connectedClients)
        {
            connectedClient.Value.PlayerObject.GetComponent<PlayerDeath>().RevivePlayerLocal();
        }
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void PlayerRevive_ClientRpc(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        pickupPlayerClient.PlayerObject.GetComponent<PlayerDeath>().RevivePlayerLocal();
    }
    #endregion
    
    [ServerRpc(RequireOwnership = false)]
    private void ToNextGameState_ServerRpc()
    {
        // Decide next state
        switch (_timeState)
        {
            case TimeState.None:
            case TimeState.BetweenDays:
                _timeState = TimeState.DayActive;
                break;
            case TimeState.DayActive:
                if (MoneyManager.Instance.CurrentDayCash >= _quota)
                    _timeState = TimeState.ShowDayResult;
                else
                    _timeState = TimeState.QuotaFailed;
                break;
            case TimeState.ShowDayResult:
                _timeState = TimeState.BetweenDays;
                break;
        }
        
        switch (_timeState)
        {
            case TimeState.BetweenDays:
                RespawnAllPlayers_ServerRpc();
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.DayActive:
                _day++;
                SpawnLoot();
                _quota += 500;
                MoneyManager.Instance.ResetCurrentCollected();
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.ShowDayResult:
                DeleteLoot();
                StartCoroutine(CountdownTimer(3, () => { ToNextGameState_ServerRpc(); }));
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.QuotaFailed:
                DeleteLoot();
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
        }
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void UpdateTimeState_ClientRpc(TimeState timeState, int day, int quota, int currentDayCash)
    {
        // Update client version of important variables
        if (!IsServer)
        {
            _timeState = timeState;
            _day = day;
            _quota = quota;
            MoneyManager.Instance.CurrentDayCash = currentDayCash;
        }
        
        switch (_timeState)
        {
            case TimeState.BetweenDays:
                _ui.CloseAllPanels();
                break;
            case TimeState.DayActive:
                _ui.UpdateDayInfoText(quota, day);
                StartCountdown();
                break;
            case TimeState.ShowDayResult:
                _ui.UpdateEndScreen(quota, currentDayCash);
                _ui.ShowEndScreen(true);
                StopCountdown();
                break;
            case TimeState.QuotaFailed:
                StopCountdown();
                Debug.Log("Game Lost");
                break;
        }
    }

    public void PlaceAttachmentPoint(int shopItemIndex, BoatAttachmentPoint boatAttachmentPoint, float rotationPlaceOffset)
    {
        int attachmentPointIndex = -1;
        for (int i = 0; i < Boat.BoatAttachmentPoints.Count; i++)
        {
            if (boatAttachmentPoint == Boat.BoatAttachmentPoints[i])
            {
                attachmentPointIndex = i;
            }
        }

        if (attachmentPointIndex >= 0)
        {
            PlaceAttachmentPoint_ServerRpc(shopItemIndex, attachmentPointIndex, rotationPlaceOffset);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlaceAttachmentPoint_ServerRpc(int shopIndex, int boatAttachmentIndex, float rotationPlaceOffset)
    {
        BoatAttachmentPoint boatAttachmentPoint = Boat.BoatAttachmentPoints[boatAttachmentIndex];
        NetworkObject no = Instantiate(ShopManager.Instance.shopList.items[shopIndex].placePrefab, 
            boatAttachmentPoint.transform.position, boatAttachmentPoint.transform.rotation).GetComponent<NetworkObject>();
        no.transform.Rotate(boatAttachmentPoint.transform.up, rotationPlaceOffset);
        no.Spawn();
        boatAttachmentPoint.heldItem.Value = no.NetworkObjectId;
        PlaceAttachmentPoint_ClientRpc(no.NetworkObjectId, boatAttachmentIndex);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void PlaceAttachmentPoint_ClientRpc(ulong networkObjectId, int boatAttachmentIndex)
    {
        BoatAttachmentPoint boatAttachmentPoint = Boat.BoatAttachmentPoints[boatAttachmentIndex];
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId,
            out NetworkObject networkLootObject);
        networkLootObject.transform.parent = boatAttachmentPoint.transform;

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

        if (IsServer)
        {
            ToNextGameState_ServerRpc();
        }
        _timeFinishedEvent?.Invoke();
    }
    
    private IEnumerator CountdownTimer(float seconds, Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback();
    }
}
