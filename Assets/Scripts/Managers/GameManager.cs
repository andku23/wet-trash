using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private LootManager _lootManager;
    [SerializeField] private UI _ui;
    [SerializeField] private float _timeFullDaySeconds;
    [SerializeField] private GameObject _boatPrefab;
    [SerializeField] private GameObject _boatSpawnLocation;
    [SerializeField] private UnityEvent<int> _timeUpdatedEvent;
    [SerializeField] public UnityEvent TimeFinishedEvent;
    [SerializeField] private UnityEvent<int> _onDayUpdatedEvent;
    [SerializeField] public UnityEvent<float> OnBreathUpdated;

    public NetworkedBoat Boat;
    private Dictionary<ulong, bool> playerWaitConfirm;
    
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
        LoadingNextDay = 5,
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

        if (IsServer)
        {
            GameObject boat = Instantiate(_boatPrefab);
            boat.transform.position = _boatSpawnLocation.transform.position;
            NetworkObject networkObject = boat.GetComponent<NetworkObject>();
            networkObject.Spawn();
        }
        
    }

    public void RequestToNextGameState()
    {
        ToNextGameState_ServerRpc();
    }

    private void WaitForPlayerResponse(Action onComplete)
    {
        playerWaitConfirm = new Dictionary<ulong, bool>();
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        foreach (var client in connectedClients)
        {
            playerWaitConfirm.Add(client.Key, false);
        }

        StartCoroutine(Co_WaitForPlayerResponse(onComplete));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerWaitResponse_ServerRpc(ulong targetPlayerNetworkObjectId)
    {
        playerWaitConfirm[targetPlayerNetworkObjectId] = true;
    }

    private IEnumerator Co_WaitForPlayerResponse(Action onComplete)
    {
        bool allPlayersResponded = false;
        while (!allPlayersResponded)
        {
            allPlayersResponded = true;
            foreach (var player in playerWaitConfirm)
            {
                if (!player.Value)
                {
                    allPlayersResponded = false;
                    break;
                }
            }
            yield return null;
        }
        onComplete?.Invoke();
    }

    public void ChangeToBuildMode(int shopItemIndex)
    {
        InteractionController.Instance.ChangeToBuildMode(shopItemIndex);
    }

    #region Player Death
    
    public void ChangeHealth(ulong targetPlayerNetworkObjectId, float newHealth)
    {
        ChangeHealth_ServerRpc(targetPlayerNetworkObjectId, newHealth);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeHealth_ServerRpc(ulong targetPlayerNetworkObjectId, float newHealth)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        pickupPlayerClient.PlayerObject.GetComponent<PlayerState>().Health.Value = newHealth;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RespawnAllPlayers_ServerRpc()
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        foreach (var connectedClient in connectedClients)
        {
            var playerState = connectedClient.Value.PlayerObject.GetComponent<PlayerState>();
            playerState.Health.Value = playerState.MAX_HEALTH;
        }
    }
    #endregion
    
    #region Game State
    [ServerRpc(RequireOwnership = false)]
    private void ToNextGameState_ServerRpc()
    {
        // Decide next state
        switch (_timeState)
        {
            case TimeState.None:
            case TimeState.BetweenDays:
                _timeState = TimeState.LoadingNextDay;
                break;
            case TimeState.LoadingNextDay:
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
        
        //On enter for next state
        switch (_timeState)
        {
            case TimeState.BetweenDays:
                RespawnAllPlayers_ServerRpc();
                _ui.PopulateShopContent_ServerRpc();
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.LoadingNextDay:
                _day++;
                _quota += 200;
                WaitForPlayerResponse(ToNextGameState_ServerRpc);
                MoneyManager.Instance.ResetCurrentCollected();
                TerrainManager.Instance.GenerateTerrain();
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.DayActive:
                SpawnLoot();
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
                _ui.CloseAllPanels(false);
                break;
            case TimeState.LoadingNextDay:
                break;
            case TimeState.DayActive:
                _ui.UpdateDayInfoText(quota, day);
                _ui.ShowDayStartPanel();
                StartCountdown();
                break;
            case TimeState.ShowDayResult:
                TimeFinishedEvent?.Invoke();
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
    
    #endregion

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
        no.transform.parent = Boat.transform;
        boatAttachmentPoint.heldItem.Value = no.NetworkObjectId;
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
    }
    
    private IEnumerator CountdownTimer(float seconds, Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback();
    }
}
