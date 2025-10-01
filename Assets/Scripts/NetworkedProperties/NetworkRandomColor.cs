using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkRandomColor : NetworkBehaviour
{
    [SerializeField] private Renderer _renderer;
    private NetworkVariable<Color> _playerColor = new NetworkVariable<Color>();
    
    public override void OnNetworkSpawn()
    {
        _playerColor.OnValueChanged += OnColorChanged;
        if (IsServer)
        {
            _playerColor.Value = Random.ColorHSV();
        }
        else
        {
            _renderer.material.SetColor("_Color", _playerColor.Value);
        }
        
    }
    
    private void OnColorChanged(Color previous, Color newColor)
    {
        _renderer.material.SetColor("_Color", newColor);
    }
}
