using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI quotaText;
    [SerializeField] private RectTransform breathParent;
    [SerializeField] private Image breathFill;
    
    [SerializeField] private Panel shopPanel;
    [SerializeField] private RectTransform shopContent;
    [SerializeField] private GameObject shopItemPrefab;
    
    [SerializeField] private Panel sharedInventoryPanel;
    [SerializeField] private RectTransform sharedInventoryContent;
    [SerializeField] private GameObject sharedInventoryPrefab;
    
    [FormerlySerializedAs("endScreen")] [SerializeField] private Panel endScreenPanel;
    [SerializeField] private TextMeshProUGUI endScreenQuotaText;
    [SerializeField] private TextMeshProUGUI endScreenCurrentCashText;

    private List<UIShopItem> uiShopItems = new List<UIShopItem>();
    
    private void Start()
    {
        CloseAllPanels(true, false);
    }

    public void ButtonEvt_CloseAllPanels()
    {
        CloseAllPanels(false);
    }

    public void CloseAllPanels(bool immediate, bool lockCursor = true)
    {
        shopPanel.FadeOut(immediate);
        sharedInventoryPanel.FadeOut(immediate);
        endScreenPanel.FadeOut(immediate);
        if(lockCursor)
            Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void ShowShopPanel(bool isVisible)
    {
        shopPanel.FadeIn(false);
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void ShowSharedInventoryPanel(bool isVisible)
    {
        sharedInventoryPanel.FadeIn(false);
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowEndScreen(bool isVisible)
    {
        endScreenPanel.FadeIn(false);
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
            //Debug.Log(shopItem.type);
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
    
    [ServerRpc(RequireOwnership = false)]
    public void PopulateShopContent_ServerRpc()
    {
        int shopSize = 4;
        List<int> fullShop = new List<int>();
        for (int i = 0; i < ShopManager.Instance.shopList.items.Length; i++)
        {
            fullShop.Add(i);
        }
        
        int[] randomizedShopList = new int[shopSize];
        for (int i = 0; i < randomizedShopList.Length; i++)
        {
            int index = Random.Range(0, fullShop.Count);
            randomizedShopList[i] = fullShop[index];
            fullShop.RemoveAt(index);
        }

        PopulateShopContent_ClientRpc(randomizedShopList);
    }

    [ClientRpc(RequireOwnership = false)]
    public void PopulateShopContent_ClientRpc(int[] randomizedShopList)
    {
        ClearShopContent();
        for (int i = 0; i < randomizedShopList.Length; i++)
        {
            int index = randomizedShopList[i];
            UIShopItem uiShopItem = Instantiate(shopItemPrefab, shopContent).GetComponent<UIShopItem>();
            uiShopItem.name.text = ShopManager.Instance.shopList.items[index].name;
            uiShopItem.price.text = "$"+ShopManager.Instance.shopList.items[index].price.ToString();
            uiShopItem.button.onClick.AddListener(() =>
            {
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
        CloseAllPanels(false);
    }
    
    #region Text Updates

    public void UpdateDayInfoText(int quota, int day)
    {
        UpdateDayText(day);
        UpdateQuotaText(quota);
    }
    
    public void UpdateEndScreen(int quota, int currentRunCash)
    {
        endScreenQuotaText.text = quota.ToString();
        endScreenCurrentCashText.text = currentRunCash.ToString();
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
    
    public void UpdateQuotaText(int quota)
    {
        quotaText.text = "Quota: $" + quota.ToString();
    }
    
    public void UpdateBreathBar(float percentage)
    {
        breathFill.fillAmount = percentage;
    }
    
    #endregion
    
    private void ClearShopContent()
    {
        Debug.Log("shop size: " + uiShopItems.Count);
        for (int i = uiShopItems.Count - 1; i >= 0; i--)
        {
            var item = uiShopItems[i];
            uiShopItems.RemoveAt(i);
            Destroy(item.gameObject);
        }
        uiShopItems.Clear();
    }
    
}
