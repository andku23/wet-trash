using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameUI : NetworkBehaviour
{
    public static GameUI Instance;
    
    [Space(10)]
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI merideumText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI quotaText;
    [SerializeField] private RectTransform breathParent;
    [SerializeField] private Image breathFill;
    [SerializeField] private HotbarItem[] hotbarItems;
    
    [Space(10)]
    [Header("Shop")]
    [SerializeField] private Panel shopPanel;
    [SerializeField] private RectTransform shopContent;
    [SerializeField] private GameObject shopItemPrefab;
    
    [Space(10)]
    [Header("Inventory")]
    [SerializeField] private Panel sharedInventoryPanel;
    [SerializeField] private RectTransform sharedInventoryContent;
    [SerializeField] private GameObject sharedInventoryPrefab;
    
    [Space(10)]
    [Header("End Screen")]
    [SerializeField] private Panel endScreenPanel;
    [SerializeField] private TextMeshProUGUI endScreenQuotaText;
    [SerializeField] private TextMeshProUGUI endScreenCurrentCashText;
    [SerializeField] private RectTransform rogueCardArea;
    
    [Space(10)]
    [Header("Start Day Screen")]
    [SerializeField] private StartDayPanel startDayPanel;
    [SerializeField] private TextMeshProUGUI startDayText;
    [SerializeField] private TextMeshProUGUI startQuotaText;
    
    [Space(10)]
    [Header("Dead Screen")]
    [SerializeField] private Panel deadPanel;

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
        
        UpdateCashText(CurrencyType.Ore, 0);
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
    
    public async Awaitable ShowDayStartPanel(StartDayPanel.Mode mode)
    {
        startDayPanel.SetMode(mode);
        Cursor.lockState = CursorLockMode.None;
        GameManager.Instance.OnUIOpened?.Invoke();
        if (!startDayPanel.IsVisible)
        {
            startDayPanel.FadeIn(false, 1.0f);
            await Awaitable.WaitForSecondsAsync(1f); 
        }
    }
    
    public async Awaitable HideDayStartPanel()
    {
        startDayPanel.FadeOut(false, 1.0f);
        GameManager.Instance.OnUIClosed?.Invoke();
        await Awaitable.WaitForSecondsAsync(1f);
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ShowRogueCards(List<RogueCard> rogueCards)
    {
        for (int i = 0; i < rogueCards.Count; i++)
        {
            rogueCards[i].transform.parent = rogueCardArea;
        }
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
            //TODO Update shop items to use prices as  well
            //MoneyManager.Instance.SubtractCash(shopItem.price);
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

    public void UpdateDayInfoText(int[] quotas, int day)
    {
        UpdateDayText(day);

        string currentQuotaText = "";
        for (int i = 0; i < quotas.Length; i++)
        {
            currentQuotaText += ((CurrencyType)i)+": " + quotas[i] + "\n";
        }
        UpdateQuotaText(currentQuotaText);
    }
    
    public void UpdateEndScreen(int[] quotas, List<int> currentRunCash, bool showCards = false)
    {
        if (showCards)
        {
            rogueCardArea.gameObject.SetActive(true);
        }
        else
        {
            rogueCardArea.gameObject.SetActive(false);
        }
        
        string quotaText = "";
        for (int i = 0; i < quotas.Length; i++)
        {
            quotaText += ((CurrencyType)i)+": " + quotas[i] + "\n";
        }
        
        string currentCashText = "";
        for (int i = 0; i < currentRunCash.Count; i++)
        {
            currentCashText += ((CurrencyType)i)+": " + currentRunCash[i] + "\n";
        }
        
        endScreenQuotaText.text = quotaText;
        endScreenCurrentCashText.text = currentCashText.ToString();
    }
    
    public void UpdateCashText(CurrencyType type, int amount)
    {
        cashText.text = "";
        var wallet = MoneyManager.Instance.Wallet;
        for (int i = 0; i < wallet.Count; i++)
        {
            cashText.text += ((CurrencyType)i).ToString() + ": " + wallet[i] + "\n";
        }
    }
    
    public void UpdateCountdownText(int timeLeft, int timeTotal)
    {
        float percentageElapsed = 1f - ((float)timeLeft / timeTotal);
        int totalScaledMinutes = (GameManager.Instance.GetDayLengthHours) * 60;
        int currentScaledMinutes = Mathf.FloorToInt(totalScaledMinutes * percentageElapsed);
        int hoursElapsed = (currentScaledMinutes / 60) + GameManager.Instance.gameData.DAY_START_HOUR;
        int minutesElapsed = (currentScaledMinutes % 60);
        int hoursElapsed12 = hoursElapsed % 12;
        string meridiem = (hoursElapsed > 12) ? "PM" : "AM";
        timeText.text = hoursElapsed12.ToString("00") + ":" + minutesElapsed.ToString("00");
        merideumText.text = meridiem;
    }
    
    public void UpdateDayText(int day)
    {
        dayText.text = "Day: " + day.ToString();
        startDayText.text = "Day: " + day.ToString();
    }
    
    public void UpdateQuotaText(string quota)
    {
        quotaText.text = "Quota: \n" + quota;
        startQuotaText.text = "Quota: \n" + quota;
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
