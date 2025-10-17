using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;
    
    public void UpdateCashText(int amount)
    {
        cashText.text = "$" + amount.ToString();
    }

    
}
