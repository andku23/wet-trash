using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class LootSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject _lootPrefab;
    [SerializeField] private int _numLoot;
    
    private List<NetworkLoot> _loot;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            for (int i = 0; i < _numLoot; i++)
            {
                _loot.Add(Instantiate(_lootPrefab, new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f)), Quaternion.identity).GetComponent<NetworkLoot>());;
            }
        }
    }
}
