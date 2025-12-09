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
    [SerializeField] private GameData sourceGameData;
    [HideInInspector] public GameData gameData;
    
    [SerializeField] private LootManager _lootManager;
    [SerializeField] private BoatManager _boatManager;
    private GameUI gameUI;
    public UnityEvent<int, int> TimeUpdatedEvent;
    public UnityEvent TimeFinishedEvent;
    public UnityEvent<int> OnDayUpdatedEvent;
    public UnityEvent<float> OnBreathUpdated;

    public Action OnUIOpened;
    public Action OnUIClosed;

    private Dictionary<ulong, bool> playerWaitConfirm_s;
    private LevelData currentLevelData_s;
    
    private TimeState _timeState;
    private Coroutine _co_TimerCountdown;
    private int _day = 0;
    private int _quota = 0;
    
    private int _currentAdditiveScene = -1;
    private bool _isLoadingScene;
    
    public int GetDayLengthHours { get {return gameData.DAY_END_HOUR - gameData.DAY_START_HOUR;}}
    
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
        
        gameData = Instantiate(sourceGameData);
    }

    public void RequestToNextGameState()
    {
        ToNextGameState_ServerRpc();
    }

    private void WaitForPlayerResponse(Action onComplete)
    {
        playerWaitConfirm_s = new Dictionary<ulong, bool>();
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        foreach (var client in connectedClients)
        {
            playerWaitConfirm_s.Add(client.Key, false);
        }
        
        Debug.Log("start wait corout");
        StartCoroutine(Co_WaitForPlayerResponse(onComplete));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PlayerWaitResponse_ServerRpc(ulong targetPlayerNetworkObjectId)
    {
        playerWaitConfirm_s[targetPlayerNetworkObjectId] = true;
    }

    private IEnumerator Co_WaitForPlayerResponse(Action onComplete)
    {
        bool allPlayersResponded = false;
        while (!allPlayersResponded)
        {
            yield return new WaitForSeconds(0.5f);
            allPlayersResponded = true;
            foreach (var player in playerWaitConfirm_s)
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

    public void ChangeToAttachmentMode(int shopItemIndex)
    {
        InteractionController.Instance.ChangeToAttachmentMode(shopItemIndex);
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
                GameUI.Instance.PopulateShopContent_ServerRpc();
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.LoadingNextDay:
                _day++;
                currentLevelData_s = new LevelData(gameData.MAX_MONSTERS_PER_DAY, gameData.MONSTER_SPAWN_PER_HOUR);
                MoneyManager.Instance.ResetCurrentCollected();
                WaitForPlayerResponse(ToNextGameState_ServerRpc);
                var clientTerrainGenData = WorldManager.Instance.GenerateClientTerrainData_S();
                WorldManager.Instance.AssignGenerationData_ClientRpc(clientTerrainGenData);
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.DayActive:
                _lootManager.DeleteAllLoot();
                _quota = Mathf.FloorToInt(_lootManager.SpawnLoot() * gameData.QUOTA_PERCENTAGE);
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.ShowDayResult:
                _lootManager.DeleteAllLoot();
                
                RogueCardPacketData[] datas = RogueEffectManager.Instance.GenerateCards_S(1, 1, gameData.NUM_ROGUE_CARDS);
                RogueEffectManager.Instance.WaitForCardVote_S(datas, ToNextGameState_ServerRpc);
                RogueEffectManager.Instance.ShowCards_ClientRpc(datas);
                UpdateTimeState_ClientRpc(_timeState, _day, _quota, MoneyManager.Instance.CurrentDayCash);
                break;
            case TimeState.QuotaFailed:
                _lootManager.DeleteAllLoot();
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
                GameUI.Instance?.CloseAllPanels(false);
                break;
            case TimeState.LoadingNextDay:
                LoadNextDay_Client();
                break;
            case TimeState.DayActive:
                GameUI.Instance.UpdateDayInfoText(quota, day);
                DayActive_Client();
                break;
            case TimeState.ShowDayResult:
                ShowDayResult_Client(quota, currentDayCash);
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

    private async Awaitable LoadNextDay_Client()
    {
        await GameUI.Instance.ShowDayStartPanel(StartDayPanel.Mode.Loading);
        await WorldManager.Instance.GenerateTerrain();
        PlayerWaitResponse_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }
    
    private async Awaitable DayActive_Client()
    {
        await GameUI.Instance.ShowDayStartPanel(StartDayPanel.Mode.StartDay);
        await Awaitable.WaitForSecondsAsync(1f);
        StartCountdown();
        await GameUI.Instance.HideDayStartPanel();
    }
    
    private async Awaitable ShowDayResult_Client(int quota, int currentDayCash)
    {
        TimeFinishedEvent?.Invoke();
        GameUI.Instance.UpdateEndScreen(quota, currentDayCash, false);
        GameUI.Instance.ShowEndScreen(true);
        await Awaitable.WaitForSecondsAsync(2f);
        GameUI.Instance.UpdateEndScreen(quota, currentDayCash, true);
        StopCountdown();
    }
    
    
    #region Day Cycle Logic
    private void StartCountdown()
    {
        StopCountdown();
        OnDayUpdatedEvent.Invoke(_day);
        _co_TimerCountdown = StartCoroutine(Co_DayTimer());
    }
    
    private IEnumerator Co_DayTimer()
    {
        int secondsRemaining = Mathf.FloorToInt(gameData.DAY_LENGTH_SECONDS);
        while (secondsRemaining > 0)
        {
            int secondsElapsed = gameData.DAY_LENGTH_SECONDS - secondsRemaining;
            TimeUpdatedEvent?.Invoke(secondsRemaining, gameData.DAY_LENGTH_SECONDS);
            if (IsServer)
            {
                CheckMonsterSpawn_S(secondsElapsed, gameData.DAY_LENGTH_SECONDS);
            }
            yield return new WaitForSeconds(1);
            secondsRemaining--;
        }

        if (IsServer)
        {
            ToNextGameState_ServerRpc();
        }
    }
    
    private void StopCountdown()
    {
        TimeUpdatedEvent?.Invoke(0, gameData.DAY_LENGTH_SECONDS);
        if(_co_TimerCountdown != null) StopCoroutine(_co_TimerCountdown);
    }

    private void CheckMonsterSpawn_S(int realSecondsPassed, int realTotalSeconds)
    {
        int currentSpawnedMonsters = WorldManager.Instance.NumSpawnedMonsters;
        if (currentSpawnedMonsters < currentLevelData_s.MAX_MONSTERS_SPAWNED)
        {
            float pctElapsed = (float)realSecondsPassed / realTotalSeconds;
            float hoursElapsed = (pctElapsed) * GetDayLengthHours;
            int expectedMonstersSpawned = Mathf.FloorToInt(hoursElapsed * currentLevelData_s.MONSTER_SPAWN_PER_HOUR);
            if (currentSpawnedMonsters < expectedMonstersSpawned)
            {
                WorldManager.Instance.SpawnRandomEnemy_S();
            }
        }
    }
    
    #endregion
    
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
