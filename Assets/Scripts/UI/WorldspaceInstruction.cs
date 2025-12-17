using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldspaceInstruction : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI instructionsText;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject iconPrefab;

    private List<GameObject> icons = new List<GameObject>();

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetText(string text)
    {
        instructionsText.text = text;
    }
    
    public void SetSpriteList(string title, Sprite[] sprites)
    {
        if (icons.Count != 0)
        {
            foreach (GameObject icon in icons)
            {
                Destroy(icon);
            }
            icons.Clear();
        }

        foreach (Sprite sprite in sprites)
        {
            GameObject go = Instantiate(iconPrefab, contentContainer);
            iconPrefab.GetComponent<Image>().sprite = sprite;
            icons.Add(go);
        }
        instructionsText.text = title;
    }
}
