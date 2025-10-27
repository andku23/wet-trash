using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private RectTransform breathParent;
    [SerializeField] private Image breathFill;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private RectTransform shopContent;
    [SerializeField] private GameObject shopItemPrefab;
    
    [SerializeField] private GameObject sharedInventoryPanel;
    [SerializeField] private RectTransform sharedInventoryContent;
    [SerializeField] private GameObject sharedInventoryPrefab;

    private List<UIShopItem> uiShopItems = new List<UIShopItem>();
    
    private void Start()
    {
        shopPanel.SetActive(false);
        sharedInventoryPanel.SetActive(false);
    }

    public void CloseAllPanels()
    {
        shopPanel.SetActive(false);
        sharedInventoryPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void UpdateCashText(int amount)
    {
        cashText.text = "$" + amount.ToString();
    }
    
    public void UpdateCountdownText(int timeLeft)
    {
        timeText.text = Mathf.FloorToInt(timeLeft/60f).ToString("00") + ":" + Mathf.FloorToInt(timeLeft%60).ToString("00");
    }
    
    public void UpdateDayText(int day)
    {
        dayText.text = "Day: " + day.ToString();
    }
    
    public void UpdateBreathBar(float percentage)
    {
        breathFill.fillAmount = percentage;
    }
    
    public void ShowShopPanel(bool isVisible)
    {
        shopPanel.SetActive(isVisible);
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void ShowSharedInventoryPanel(bool isVisible)
    {
        sharedInventoryPanel.SetActive(isVisible);
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void ClearSharedInventoryUI()
    {
        for (int i = sharedInventoryContent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(sharedInventoryContent.transform.GetChild(i).gameObject);
        }
    }

    public void PopulateSharedInventoryUI(List<int> boughtItems)
    {
        for (int i = 0; i < boughtItems.Count; i++)
        {
            int boughtItemIndex = i;
            int shopItemIndex = boughtItems[i];
            UIShopItem uiShopItem = Instantiate(sharedInventoryPrefab, sharedInventoryContent).GetComponent<UIShopItem>();
            ShopItem shopItem = ShopManager.Instance.shopList.items[shopItemIndex];
            uiShopItem.name.text = shopItem.name;
            Debug.Log(shopItem.type);
            uiShopItem.price.text = ShopManager.Instance.ShopItemTypeLookup[shopItem.type];
            uiShopItem.button.onClick.AddListener(() =>
            {
                OnSharedInventoryButtonClick(shopItemIndex, boughtItemIndex);
            });
            uiShopItems.Add(uiShopItem);
        }
    }
    
    public void PopulateShopContent()
    {
        for (int i = 0; i < ShopManager.Instance.shopList.items.Length; i++)
        {
            int index = i;
            UIShopItem uiShopItem = Instantiate(shopItemPrefab, shopContent).GetComponent<UIShopItem>();
            uiShopItem.name.text = ShopManager.Instance.shopList.items[i].name;
            uiShopItem.price.text = "$"+ShopManager.Instance.shopList.items[i].price.ToString();
            uiShopItem.button.onClick.AddListener(() =>
            {
                // TODO when I change this to be randomized, make sure the indexes are correct
                OnShopButtonClick(index);
            });
            uiShopItems.Add(uiShopItem);
        }
    }
    
    public void OnShopButtonClick(int shopItemIndex)
    {
        ShopItem shopItem = ShopManager.Instance.shopList.items[shopItemIndex];
        if (MoneyManager.Instance.Cash >= shopItem.price)
        {
            MoneyManager.Instance.SubtractCash(shopItem.price);
            ShopManager.Instance.PurchaseItem(shopItem, shopItemIndex);
        }
    }
    
    public void OnSharedInventoryButtonClick(int shopItemIndex, int boughtItemIndex)
    {
        ShopItem shopItem = ShopManager.Instance.shopList.items[shopItemIndex];
        ShopManager.Instance.boughtItems.RemoveAt(boughtItemIndex);
        ClearSharedInventoryUI();
        PopulateSharedInventoryUI(ShopManager.Instance.boughtItems);
        GameManager.Instance.ChangeToBuildMode(shopItemIndex);
        CloseAllPanels();
    }
    
    public void ClearShopContent()
    {
        //loop through uiShopItems for this
    }
    
}
