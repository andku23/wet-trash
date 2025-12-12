using TMPro;
using UnityEngine;

public class GameDataDisplay : MonoBehaviour
{
    
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI quotaText;
    [SerializeField] private TextMeshProUGUI walletText;
    private void Start()
    {
        GameManager.Instance.OnQuotaUpdated.AddListener(OnQuotaUpdated);
        GameManager.Instance.OnDayUpdatedEvent.AddListener(OnDayUpdated);
        MoneyManager.Instance.OnWalletChanged.AddListener(OnWalletUpdated);

        for (int i = 0; i < MoneyManager.Instance.Wallet.Count; i++)
        {
            OnWalletUpdated((CurrencyType) i, MoneyManager.Instance.Wallet[i]);
        }
    }
    
    private void OnDestroy()
    {
        GameManager.Instance.OnQuotaUpdated.RemoveListener(OnQuotaUpdated);
        GameManager.Instance.OnDayUpdatedEvent.RemoveListener(OnDayUpdated);
        MoneyManager.Instance.OnWalletChanged.RemoveListener(OnWalletUpdated);
    }

    private void OnQuotaUpdated(int[] newQuotas)
    {
        quotaText.text = "Quota:\n";
        for (int i = 0; i < newQuotas.Length; i++)
        {
            quotaText.text += ((CurrencyType)i).ToString() + ": " + newQuotas[i] + "\n";
        }
    }

    private void OnDayUpdated(int day)
    {
        dayText.text = "Day " + day.ToString();
    }

    private void OnWalletUpdated(CurrencyType type, int amount)
    {
        walletText.text = "Wallet:\n";
        var wallet = MoneyManager.Instance.Wallet;
        for (int i = 0; i < wallet.Count; i++)
        {
            walletText.text += ((CurrencyType)i).ToString() + ": " + wallet[i] + "\n";
        }
    }
}
