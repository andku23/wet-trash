using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private RectTransform breathParent;
    [SerializeField] private Image breathFill;
    
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

    
}
