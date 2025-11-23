using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private LootManager _lootManager;
    [SerializeField] private BoatManager _boatManager;
    [SerializeField] private UI _ui;
    [SerializeField] private float _timeFullDaySeconds;
    [SerializeField] public UnityEvent<int> TimeUpdatedEvent;
    [SerializeField] public UnityEvent TimeFinishedEvent;
    [SerializeField] public UnityEvent<int> OnDayUpdatedEvent;
    [SerializeField] public UnityEvent<float> OnBreathUpdated;

    public Action OnUIOpened;
    public Action OnUIClosed;

    private Dictionary<ulong, bool> playerWaitConfirm;
    
    private TimeState _timeState;
    private Coroutine _co_TimerCountdown;
    private int _day = 0;
    private int _quota = 0;

    public int INCREMENT_QUOTA;
    private int _currentAdditiveScene = -1;
    private bool _isLoadingScene;
    
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
            _boatManager.SpawnBoatServer();
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

        
        Debug.Log("start wait corout");
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
            yield return new WaitForSeconds(0.5f);
            allPlayersResponded = true;
            foreach (var player in playerWaitConfirm)
            {
                if (!player.Value)
                {
                    allPlayersResponded = false;
                    break;
                }
            }
            Debug.Log("waiting for players");
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
                _timeState = TimeState.BetweenDays;
                break;
            case TimeState.BetweenDays:
            case TimeState.QuotaFailed:
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
                UI.Instance.PopulateShopContent_ServerRpc();
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.LoadingNextDay:
                _day++;
                _quota += INCREMENT_QUOTA;
                MoneyManager.Instance.ResetCurrentCollected();
                WaitForPlayerResponse(ToNextGameState_ServerRpc);
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                TerrainManager.Instance.GenerateTerrain_ServerRpc();
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
                RespawnAllPlayers_ServerRpc();
                _day = 0;
                _quota = 0;
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
                UI.Instance.CloseAllPanels(false);
                break;
            case TimeState.LoadingNextDay:
                break;
            case TimeState.DayActive:
                UI.Instance.UpdateDayInfoText(quota, day);
                UI.Instance.ShowDayStartPanel();
                StartCountdown();
                break;
            case TimeState.ShowDayResult:
                TimeFinishedEvent?.Invoke();
                UI.Instance.UpdateEndScreen(quota, currentDayCash);
                UI.Instance.ShowEndScreen(true);
                StopCountdown();
                break;
            case TimeState.QuotaFailed:
                StopCountdown();
                break;
        }
    }
    
    #endregion
    
    #region Player Changes

    [ServerRpc]
    public void ChangeAllPlayerControlModes_ServerRpc(PlayerController.ControlModeEnum mode)
    {
        ChangeAllPlayerControlModes_ClientRpc(mode);
    }
    
    [ClientRpc]
    public void ChangeAllPlayerControlModes_ClientRpc(PlayerController.ControlModeEnum mode)
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().ChangeControlMode(mode);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerPositions_ServerRpc(ulong[] playerIds, Vector3 position, Quaternion rotation)
    {
        SetPlayerPositions_ClientRpc(playerIds, position, rotation);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void SetPlayerPositions_ClientRpc(ulong[] playerIds, Vector3 position, Quaternion rotation)
    {
        bool isIncludedInList = false;
        for (int i = 0; i < playerIds.Length; i++)
        {
            if (playerIds[i] == NetworkManager.Singleton.LocalClientId)
            {
                isIncludedInList = true;
                break;
            }
        }

        if (isIncludedInList)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            player.transform.position = position;
            player.transform.rotation = rotation;
        }
        
    }
    
    #endregion
    
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
        OnDayUpdatedEvent.Invoke(_day);
        _co_TimerCountdown = StartCoroutine(Co_TimerCountdown());
    }
    
    private void StopCountdown()
    {
        TimeUpdatedEvent?.Invoke(0);
        if(_co_TimerCountdown != null) StopCoroutine(_co_TimerCountdown);
    }
    
    private IEnumerator Co_TimerCountdown()
    {
        int secondsRemaining = Mathf.FloorToInt(_timeFullDaySeconds);
        while (secondsRemaining > 0)
        {
            TimeUpdatedEvent?.Invoke(secondsRemaining);
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
    
    #region Scene Change Logic

    [ServerRpc(RequireOwnership = false)]
    public void ChangeScene_ServerRpc(int buildIndex)
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;
        
        if (_currentAdditiveScene != -1)
        {
            NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted += OnUnloadFinish;
            NetworkManager.Singleton.SceneManager.UnloadScene(SceneManager.GetSceneByBuildIndex(_currentAdditiveScene));
            _currentAdditiveScene = buildIndex;
            
        }
        else
        {
            _currentAdditiveScene = buildIndex;
            OnUnloadFinish("", LoadSceneMode.Additive, null, null);
        }
    }

    private void OnUnloadFinish(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted -= OnUnloadFinish;
        string gameScenePath = SceneUtility.GetScenePathByBuildIndex(_currentAdditiveScene);
        string gameSceneName = System.IO.Path.GetFileNameWithoutExtension(gameScenePath);
        Debug.Log(gameSceneName);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
        _isLoadingScene = false;
    }

    #endregion
}
