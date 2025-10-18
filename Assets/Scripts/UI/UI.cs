using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI timeText;
    
    public void UpdateCashText(int amount)
    {
        cashText.text = "$" + amount.ToString();
    }
    
    public void UpdateCountdownText(int timeLeft)
    {
        timeText.text = Mathf.FloorToInt(timeLeft/60f).ToString("00") + ":" + Mathf.FloorToInt(timeLeft%60).ToString("00");
    }

    
}
