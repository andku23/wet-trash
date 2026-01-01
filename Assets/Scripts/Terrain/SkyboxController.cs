using System;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [SerializeField] private Material day;
    [SerializeField] private Material twilight;
    [SerializeField] private Material night;
    
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Transform sunLight;
    
    [SerializeField] private List<SkyboxTimeSpread> skyboxTimes;
    
    private void Start()
    {
        GameManager.Instance.TimeUpdatedEvent.AddListener(OnTimeUpdated);
    }

    private void OnTimeUpdated(int timeRemaining, int timeTotal)
    {
        float currentNormalizedStartTime = (float) GameManager.Instance.gameData.DAY_START_HOUR / 24;
        float currentNormalizedEndTime = (float) GameManager.Instance.gameData.DAY_END_HOUR / 24;
        float dayHourLength = GameManager.Instance.gameData.DAY_END_HOUR - GameManager.Instance.gameData.DAY_START_HOUR;
        float currentNormalizedTime = (((float)timeTotal-timeRemaining)/timeTotal) * 
                                      (currentNormalizedEndTime-currentNormalizedStartTime)
                                      + currentNormalizedStartTime;
        foreach (SkyboxTimeSpread skyboxTime in skyboxTimes)
        {
            if (currentNormalizedTime > skyboxTime.normalizedStartTime &&
                currentNormalizedTime <= skyboxTime.normalizedEndTime)
            {
                skyboxMaterial.CopyMatchingPropertiesFromMaterial(skyboxTime.skyboxMaterial);
            }
        }
        
        Debug.Log(currentNormalizedTime);

        sunLight.transform.localEulerAngles = new Vector3(currentNormalizedTime*360f-90, 0f, 0f);

    }
}

[Serializable]
public class SkyboxTimeSpread
{
    public float normalizedStartTime;
    public float normalizedEndTime;
    public Material skyboxMaterial;
}
