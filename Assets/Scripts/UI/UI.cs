using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private RectTransform breathParent;
    [SerializeField] private Image breathFill;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private RectTransform shopContent;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private ShopItems shopList;

    private List<UIShopItem> uiShopItems = new List<UIShopItem>();
    
    private void Start()
    {
        SpawnShopContent();
        shopPanel.SetActive(false);
    }
    
    public void UpdateCashText(int amount)
    {
        cashText.text = "$" + amount.ToString();
    }
    
    public void UpdateCountdownText(int timeLeft)
    {
        timeText.text = Mathf.FloorToInt(timeLeft/60f).ToString("00") + ":" + Mathf.FloorToInt(timeLeft%60).ToString("00");
    }
    
    public void UpdateBreathBar(float percentage)
    {
        breathFill.fillAmount = percentage;
    }
    
    public void ShowShopPanel(bool isVisible)
    {
        shopPanel.SetActive(isVisible);
    }
    
    public void SpawnShopContent()
    {
        for (int i = 0; i < shopList.items.Length; i++)
        {
            int index = i;
            UIShopItem uiShopItem = Instantiate(shopItemPrefab, shopContent).GetComponent<UIShopItem>();
            uiShopItem.name.text = shopList.items[i].name;
            uiShopItem.price.text = "$"+shopList.items[i].price.ToString();
            uiShopItem.button.onClick.AddListener(() =>
            {
                OnShopButtonClick(index);
            });
            uiShopItems.Add(uiShopItem);
        }
    }
    
    public void OnShopButtonClick(int index)
    {
        ShopItem shopItem = shopList.items[index];
        if (MoneyManager.Instance.Cash >= shopItem.price)
        {
            MoneyManager.Instance.SubtractCash(shopItem.price);
        }
    }
    
    public void ClearShopContent()
    {
        //loop through uiShopItems for this
    }
    
}
