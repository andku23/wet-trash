using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private LootManager _lootManager;
    [SerializeField] private float _timeFullDaySeconds;
    [SerializeField] private UnityEvent<int> _timeUpdatedEvent;
    [SerializeField] private UnityEvent _timeFinishedEvent;
    
    private bool _isDayStarted = false;
    private Coroutine _co_TimerCountdown;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void RequestToggleDay()
    {
        ToggleBeginDay_ServerRpc(!_isDayStarted);
    }

    public void RequestStartDay()
    {
        ToggleBeginDay_ServerRpc(true);
    }
    
    public void RequestEndDay()
    {
        ToggleBeginDay_ServerRpc(false);
    }
    
    public void RequestSpawnLoot()
    {
        SpawnLoot_ServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ToggleBeginDay_ServerRpc(bool isDayStarted)
    {
        if (_isDayStarted == isDayStarted) return;
        _isDayStarted = isDayStarted;
        if (_isDayStarted)
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
        _isDayStarted = isDayStarted;
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
