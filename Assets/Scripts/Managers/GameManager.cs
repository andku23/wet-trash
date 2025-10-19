using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private LootManager _lootManager;
    [SerializeField] private float _timeFullDaySeconds;
    [SerializeField] private UnityEvent<int> _timeUpdatedEvent;
    [SerializeField] private UnityEvent _timeFinishedEvent;
    [SerializeField] public UnityEvent<float> OnBreathUpdated;
    
    private bool _isDayActive = false;
    private Coroutine _co_TimerCountdown;
    
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
        ToggleBeginDay_ServerRpc(!_isDayActive);
    }

    public void RequestStartDay()
    {
        ToggleBeginDay_ServerRpc(true);
    }
    
    public void RequestEndDay()
    {
        ToggleBeginDay_ServerRpc(false);
    }

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
    
    [ServerRpc(RequireOwnership = false)]
    private void ToggleBeginDay_ServerRpc(bool isDayStarted)
    {
        if (_isDayActive == isDayStarted) return;
        _isDayActive = isDayStarted;
        if (_isDayActive)
        {
            SpawnLoot();
        }
        else
        {
            DeleteLoot();
        }
        ToggleBeginDay_ClientRpc(isDayStarted);
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void ToggleBeginDay_ClientRpc(bool isDayStarted)
    {
        _isDayActive = isDayStarted;
        if (isDayStarted)
        {
            StartCountdown();
        }
        else
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
