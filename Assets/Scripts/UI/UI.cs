using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UI : NetworkBehaviour
{
    public static UI Instance;
    
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
    
    [SerializeField] private Panel endScreenPanel;
    [SerializeField] private TextMeshProUGUI endScreenQuotaText;
    [SerializeField] private TextMeshProUGUI endScreenCurrentCashText;
    
    [SerializeField] private Panel startDayPanel;
    [SerializeField] private TextMeshProUGUI startDayText;
    [SerializeField] private TextMeshProUGUI startQuotaText;
    
    [SerializeField] private Panel deadPanel;

    [SerializeField] private HotbarItem[] hotbarItems;

    private List<UIShopItem> uiShopItems = new List<UIShopItem>();
    
    private void Start()
    {
        if(Instance == null) Instance = this;
        CloseAllPanels(true, false);
        
    }

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.TimeUpdatedEvent.AddListener(UpdateCountdownText);
        GameManager.Instance.OnDayUpdatedEvent.AddListener(UpdateDayText);
        GameManager.Instance.OnBreathUpdated.AddListener(UpdateBreathBar);
        MoneyManager.Instance.OnCashChanged.AddListener(UpdateCashText);
    }

    private void OnDestroy()
    {
        if(Instance == this) Instance = null;
        GameManager.Instance.TimeUpdatedEvent.RemoveListener(UpdateCountdownText);
        GameManager.Instance.OnDayUpdatedEvent.RemoveListener(UpdateDayText);
        GameManager.Instance.OnBreathUpdated.RemoveListener(UpdateBreathBar);
        MoneyManager.Instance.OnCashChanged.RemoveListener(UpdateCashText);
    }
    
    public void SetActiveHotbarItem(int hotbarIndex)
    {
        for (int i = 0; i < hotbarItems.Length; i++)
        {
            hotbarItems[i].SetActiveItem(false);
        }
        hotbarItems[hotbarIndex].SetActiveItem(true);
    }

    public void AddHotbarItem(int hotbarIndex, int itemIndex)
    {
        hotbarItems[hotbarIndex].ItemImage.sprite = LootManager.Instance.LootIndextoData(itemIndex).hotbarIcon;
    }
    
    public void RemoveHotbarItem(int hotbarIndex)
    {
        hotbarItems[hotbarIndex].ItemImage.sprite = null;
    }

    public void ButtonEvt_CloseAllPanels()
    {
        CloseAllPanels(false);
    }

    public void CloseAllPanels(bool immediate, bool lockCursor = true)
    {
        GameManager.Instance.OnUIClosed?.Invoke();
        shopPanel.FadeOut(immediate);
        sharedInventoryPanel.FadeOut(immediate);
        endScreenPanel.FadeOut(immediate);
        startDayPanel.FadeOut(immediate);
        deadPanel.FadeOut(immediate);
        if(lockCursor)
            Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void ShowDayStartPanel()
    {
        StartCoroutine(Co_ShowDayStartPanel());
    }

    private IEnumerator Co_ShowDayStartPanel()
    {
        startDayPanel.FadeIn(false, 1.0f);
        Cursor.lockState = CursorLockMode.None;
        GameManager.Instance.OnUIOpened?.Invoke();
        yield return new WaitForSeconds(1f);
        startDayPanel.FadeOut(false, 1.0f);
        Cursor.lockState = CursorLockMode.Locked;
        GameManager.Instance.OnUIClosed?.Invoke();
    }
    
    public void ShowShopPanel(bool isVisible)
    {
        shopPanel.FadeIn(false);
        Cursor.lockState = CursorLockMode.None;
        GameManager.Instance.OnUIOpened?.Invoke();
    }
    
    public void ShowDeadPanel(bool isVisible)
    {
        deadPanel.FadeIn(false);
        Cursor.lockState = CursorLockMode.None;
        GameManager.Instance.OnUIOpened?.Invoke();
    }
    
    public void ShowSharedInventoryPanel(bool isVisible)
    {
        sharedInventoryPanel.FadeIn(false);
        Cursor.lockState = CursorLockMode.None;
        GameManager.Instance.OnUIOpened?.Invoke();
    }

    public void ShowEndScreen(bool isVisible)
    {
        endScreenPanel.FadeIn(false);
        Cursor.lockState = CursorLockMode.None;
        GameManager.Instance.OnUIOpened?.Invoke();
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
            ShopItemTypeData shopItemData = ShopManager.Instance.ShopItemTypeLookup[shopItem.type];
            uiShopItem.background.color = shopItemData.color;
            uiShopItem.name.text = shopItem.name;
            //Debug.Log(shopItem.type);
            uiShopItem.price.text = "$" + shopItem.price.ToString();
            uiShopItem.button.onClick.AddListener(() =>
            {
                OnSharedInventoryButtonClick(shopItemIndex, boughtItemIndex);
            });
            uiShopItems.Add(uiShopItem);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void PopulateShopContent_ServerRpc()
    {
        int shopSize = ShopManager.Instance.shopList.items.Length;
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
            ShopItem shopItem = ShopManager.Instance.shopList.items[index];
            ShopItemTypeData shopItemData = ShopManager.Instance.ShopItemTypeLookup[shopItem.type];
            UIShopItem uiShopItem = Instantiate(shopItemPrefab, shopContent).GetComponent<UIShopItem>();
            uiShopItem.name.text = shopItem.name;
            uiShopItem.background.color = shopItemData.color;
            uiShopItem.price.text = "$"+shopItem.price.ToString();
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
        if (shopItem.type == ShopItemType.BoatAttachment)
        {
            GameManager.Instance.ChangeToAttachmentMode(shopItemIndex);
        } else if (shopItem.type == ShopItemType.BoatPart)
        {
            GameManager.Instance.ChangeToBuildMode(shopItemIndex);
        }
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
        startDayText.text = "Day: " + day.ToString();
    }
    
    public void UpdateQuotaText(int quota)
    {
        quotaText.text = "Quota: $" + quota.ToString();
        startQuotaText.text = "Quota: " + quota.ToString();
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
