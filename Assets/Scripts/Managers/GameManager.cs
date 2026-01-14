using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    
    [SerializeField] private TextMeshProUGUI _debugText;
    public PlayerAudioSource AudioSource;
    
    private GameUI gameUI;
    public UnityEvent<int, int> TimeUpdatedEvent;
    public UnityEvent TimeFinishedEvent;
    public UnityEvent<int> OnDayUpdatedEvent;
    public UnityEvent<float> OnBreathUpdated;
    public UnityEvent<int[]> OnQuotaUpdated;
    
    public Action OnUIOpened;
    public Action OnUIClosed;

    private Dictionary<ulong, bool> playerWaitConfirm_s;
    private LevelData currentLevelData_s;

    private TimeState _timeState;
    private Coroutine _co_TimerCountdown;

    private int _day = 0;

    //private int _quota = 0;
    private int[] _quotas_sc;

    private int _currentAdditiveScene = -1;
    private bool _isLoadingScene;

    public int GetDayLengthHours
    {
        get { return gameData.DAY_END_HOUR - gameData.DAY_START_HOUR; }
    }
    
    public TimeState GetTimeState {get { return _timeState; }}

    public enum TimeState
    {
        None = 0,
        DayActive = 1,
        ShowDayResult = 2,
        BetweenDays = 3,
        QuotaFailed = 4,
        LoadingTerrain = 5,
        LoadingTerrainStructures = 6
    }

    public static GameManager Instance;

    private void Awake()
    {
        _quotas_sc = new int[VarietyUtilities.GetInitializedCurrencySize()];
    }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        gameData = Instantiate(sourceGameData);
    }

    public override void OnNetworkSpawn()
    {
    }

    public void RequestToNextGameState()
    {
        ToNextGameState_ServerRpc();
    }

    private async Awaitable WaitForPlayerResponse_S(Action onComplete)
    {
        if (playerWaitConfirm_s != null)
        {
            playerWaitConfirm_s.Clear();
        }
        playerWaitConfirm_s = new Dictionary<ulong, bool>();
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        foreach (var client in connectedClients)
        {
            playerWaitConfirm_s.Add(client.Key, false);
        }
        
        bool allPlayersResponded = false;
        while (!allPlayersResponded)
        {
            allPlayersResponded = true;
            foreach (var player in playerWaitConfirm_s)
            {
                if (!player.Value)
                {
                    allPlayersResponded = false;
                    break;
                }
            }
            await Awaitable.WaitForSecondsAsync(0.5f);
            
            //Debug.Log("waiting for players");
        }
        onComplete?.Invoke();
    }

    private void ResetQuotas_S()
    {
        for (int i = 0; i < _quotas_sc.Length; i++)
        {
            _quotas_sc[i] = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerWaitResponse_ServerRpc(ulong targetPlayerNetworkObjectId)
    {
        playerWaitConfirm_s[targetPlayerNetworkObjectId] = true;
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
    public void ChangePlayerPosition_ServerRpc(ulong targetPlayerNetworkObjectId, Vector3 position)
    {
        ChangePlayerPosition_ClientRpc(targetPlayerNetworkObjectId, position);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void ChangePlayerPosition_ClientRpc(ulong targetPlayerNetworkObjectId, Vector3 position)
    {
        if (NetworkManager.LocalClientId != targetPlayerNetworkObjectId) return;
        NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId].PlayerObject.transform.position = position;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeHealth_ServerRpc(ulong targetPlayerNetworkObjectId, float newHealth)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        pickupPlayerClient.PlayerObject.GetComponent<PlayerStateData>().Health.Value = newHealth;
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RespawnAllPlayers_ServerRpc()
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        foreach (var connectedClient in connectedClients)
        {
            var playerState = connectedClient.Value.PlayerObject.GetComponent<PlayerStateData>();
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
                _timeState = TimeState.LoadingTerrain;
                break;
            case TimeState.LoadingTerrain:
                _timeState = TimeState.LoadingTerrainStructures;
                break;
            case TimeState.LoadingTerrainStructures:
                _timeState = TimeState.DayActive;
                break;
            case TimeState.DayActive:
                bool hasMetQuotas = true;
                var currentDayCurrencies = MoneyManager.Instance.CurrentDayCash_C;
                for (int i = 0; i < _quotas_sc.Length; i++)
                {
                    if (_quotas_sc[i] > currentDayCurrencies[i])
                    {
                        hasMetQuotas = false;
                    }
                }
                
                if (hasMetQuotas)
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
                UpdateTimeState_ClientRpc(_timeState, _day, _quotas_sc);
                break;
            case TimeState.LoadingTerrain:
                _day++;
                currentLevelData_s = new LevelData(gameData.MAX_MONSTERS_PER_DAY, gameData.MONSTER_SURFACE_SPAWN_PER_HOUR,
                                                    gameData.MONSTER_DUNGEON_SPAWN_PER_HOUR);
                var clientTerrainGenData = WorldManager.Instance.GenerateClientTerrainData_S();
                WorldManager.Instance.AssignTerrainGenerationData_ClientRpc(clientTerrainGenData);
                WaitForPlayerResponse_S(ToNextGameState_ServerRpc);
                UpdateTimeState_ClientRpc(_timeState, _day, _quotas_sc);
                break;
            case TimeState.LoadingTerrainStructures:
                var dungeonGenData = DungeonManager.Instance.GenerateDungeonAndCreateSteps_S();
                DungeonManager.Instance.AssignDungeonGenData_ClientRpc(dungeonGenData);
                
                var clientStructureGenData = WorldManager.Instance.GenerateClientStructureData_S(dungeonGenData.DoorPositions.Length);
                WorldManager.Instance.AssignStructureGenerationData_ClientRpc(clientStructureGenData);
                WaitForPlayerResponse_S(ToNextGameState_ServerRpc);
                UpdateTimeState_ClientRpc(_timeState, _day, _quotas_sc);
                break;
            case TimeState.DayActive:
                DungeonManager.Instance.GenerateSurfaceDoors_S();
                
                var lootValue = _lootManager.SpawnLoot_S();
                foreach (var item in lootValue)
                {
                    _quotas_sc[(int)item.Key] += item.Value;
                }

                for (int i = 0; i < _quotas_sc.Length; i++)
                {
                    _quotas_sc[i] = Mathf.FloorToInt(_quotas_sc[i] * gameData.QUOTA_PERCENTAGE);
                }

                UpdateTimeState_ClientRpc(_timeState, _day, _quotas_sc);
                break;
            case TimeState.ShowDayResult:
                _lootManager.DeleteAllLoot_S();
                RogueCardPacketData[] datas = RogueEffectManager.Instance.GenerateCards_S(1, 1, gameData.NUM_ROGUE_CARDS);
                RogueEffectManager.Instance.WaitForCardVote_S(datas, ToNextGameState_ServerRpc);
                RogueEffectManager.Instance.ShowCards_ClientRpc(datas);
                UpdateTimeState_ClientRpc(_timeState, _day, _quotas_sc);
                break;
            case TimeState.QuotaFailed:
                _lootManager.DeleteAllLoot_S();
                RespawnAllPlayers_ServerRpc();
                _day = 0;
                ResetQuotas_S();
                UpdateTimeState_ClientRpc(_timeState, _day, _quotas_sc);
                break;
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void UpdateTimeState_ClientRpc(TimeState timeState, int day, int[] quota)
    {
        // Update client version of important variables
        if (!IsServer)
        {
            _timeState = timeState;
            _day = day;
            _quotas_sc = quota;
            //MoneyManager.Instance.CurrentDayCash = currentDayCash;
        }
        OnQuotaUpdated.Invoke(_quotas_sc);
        
        switch (_timeState)
        {
            case TimeState.BetweenDays:
                GameUI.Instance?.CloseAllPanels(false);
                break;
            case TimeState.LoadingTerrain:
                MoneyManager.Instance.ResetCurrentCollected_C();
                LoadTerrain_Client();
                break;
            case TimeState.LoadingTerrainStructures:
                LoadTerrainStructures_Client();
                break;
            case TimeState.DayActive:
                GameUI.Instance.UpdateDayInfoText(_quotas_sc, day);
                DayActive_Client();
                break;
            case TimeState.ShowDayResult:
                ShowDayResult_Client(_quotas_sc, MoneyManager.Instance.CurrentDayCash_C);
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

    private async Awaitable LoadTerrain_Client()
    {
        await GameUI.Instance.ShowDayStartPanel(StartDayPanel.Mode.Loading);
        await WorldManager.Instance.BeginTerrainGeneration_C();
        Debug.Log("Generating terrain finished");
        PlayerWaitResponse_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }
    
    private async Awaitable LoadTerrainStructures_Client()
    {
        await GameUI.Instance.ShowDayStartPanel(StartDayPanel.Mode.Loading);
        await WorldManager.Instance.BeginStructureGeneration_C();
        await DungeonManager.Instance.BeginDungeonGeneration_C();
        
        Debug.Log("Generating structures finished");
        PlayerWaitResponse_ServerRpc(NetworkManager.Singleton.LocalClientId);
    }
    
    
    private async Awaitable DayActive_Client()
    {
        await GameUI.Instance.ShowDayStartPanel(StartDayPanel.Mode.StartDay);
        await Awaitable.WaitForSecondsAsync(1f);
        StartCountdown();
        await GameUI.Instance.HideDayStartPanel();
    }
    
    private async Awaitable ShowDayResult_Client(int[] quotas, List<int> currentDayCash)
    {
        TimeFinishedEvent?.Invoke();
        GameUI.Instance.UpdateEndScreen(quotas, currentDayCash, false);
        GameUI.Instance.ShowEndScreen(true);
        await Awaitable.WaitForSecondsAsync(2f);
        GameUI.Instance.UpdateEndScreen(quotas, currentDayCash, true);
        StopCountdown();
    }
    
    
    #region Day Cycle Logic
    private void StartCountdown()
    {
        StopCountdown();
        OnDayUpdatedEvent.Invoke(_day);
        // Spawn Initial Monsters
        if (IsServer)
        {
            for (int i = 0; i < GameManager.Instance.gameData.MONSTER_SURFACE_SPAWN_INITIAL; i++)
            {
                WorldManager.Instance.SpawnRandomEnemy_S();
            }
        }
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
        int currentOverworldSpawnedMonsters = 0;
        int currentDungeonSpawnedMonsters = 0;

        for (int i = 0; i < WorldManager.Instance.SpawnedEnemies_S.Count; i++)
        {
            switch (WorldManager.Instance.SpawnedEnemies_S[i].SpawnArea_S)
            {
                case BaseEnemy.SpawnAreaType.Overworld:
                    currentOverworldSpawnedMonsters++;
                    break;
                case BaseEnemy.SpawnAreaType.Dungeon:
                    currentDungeonSpawnedMonsters++;
                    break;
            }
        }
        
        if (currentOverworldSpawnedMonsters + currentDungeonSpawnedMonsters < currentLevelData_s.MAX_MONSTERS_SPAWNED)
        {
            float pctElapsed = (float)realSecondsPassed / realTotalSeconds;
            float hoursElapsed = (pctElapsed) * GetDayLengthHours;
            int expectedSurfaceMonstersSpawned = Mathf.FloorToInt(hoursElapsed * currentLevelData_s.MONSTER_OVERWORLD_SPAWN_PER_HOUR);
            int expectedDungeonMonstersSpawned = Mathf.FloorToInt(hoursElapsed * currentLevelData_s.MONSTER_DUNGEON_SPAWN_PER_HOUR);
            
            if (currentOverworldSpawnedMonsters < expectedSurfaceMonstersSpawned)
            {
                WorldManager.Instance.SpawnRandomEnemy_S();
            }
            
            if (currentDungeonSpawnedMonsters < expectedDungeonMonstersSpawned)
            {
                WorldManager.Instance.SpawnRandomDungeonEnemy_S();
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
            //SetActiveScene_ClientRpc(buildIndex);
        }
        else
        {
            _currentAdditiveScene = buildIndex;
            OnUnloadFinish("", LoadSceneMode.Additive, null, null);
            //SetActiveScene_ClientRpc(buildIndex);
        }
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void SetActiveScene_ClientRpc(int buildIndex)
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(buildIndex));
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
