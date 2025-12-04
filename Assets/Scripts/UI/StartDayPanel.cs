using UnityEngine;

public class StartDayPanel : Panel
{
    [SerializeField] private GameObject loading;
    [SerializeField] private GameObject start;
    
    public enum Mode
    {
        Loading,
        StartDay
    }

    private void Start()
    {
        start.SetActive(false);
        loading.SetActive(false);
    }

    public void SetMode(Mode mode)
    {
        switch (mode)
        {
            case Mode.StartDay:
                start.SetActive(true);
                loading.SetActive(false);
                break;
            case Mode.Loading:
                start.SetActive(false);
                loading.SetActive(true);
                break;
        }
    }
}
