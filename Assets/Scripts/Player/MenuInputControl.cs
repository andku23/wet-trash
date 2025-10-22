using StarterAssets;
using UnityEngine;

public class MenuInputControl : MonoBehaviour
{
    private StarterAssetsInputs _input;
    
    void Start()
    {
        _input = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
    }

    void Update()
    {
        if (_input.inventory)
        {
            ShopManager.Instance.ViewBoughtInventory();
            _input.inventory = false;
        }
    }
}
