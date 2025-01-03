using Unity.Netcode;
using UnityEngine;

public class NetworkRandomColor : NetworkBehaviour
{
    [SerializeField] private Renderer _renderer;
    private NetworkVariable<Color> _playerColor = new NetworkVariable<Color>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _playerColor.OnValueChanged += OnColorChanged;
    }
    
    public override void  OnNetworkSpawn()
    {
        if (IsOwner)
        {
            RequestServerColorChangeRpc();
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
    
    [Rpc(SendTo.Server)]
    private void RequestServerColorChangeRpc()
    {
        _playerColor.Value = Random.ColorHSV();
        
    }
}
