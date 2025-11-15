using UnityEngine;
using UnityEngine.UI;

public class HotbarItem : MonoBehaviour
{
    public Image ItemImage;
    public Image OutlineImage;
    public bool IsActiveItem;
    
    public void SetActiveItem(bool isActiveItem)
    {
        IsActiveItem = isActiveItem;
        OutlineImage.gameObject.SetActive(isActiveItem);
    }
}
