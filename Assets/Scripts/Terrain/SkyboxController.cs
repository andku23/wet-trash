using System;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Transform sunLight;
    
    [SerializeField] private List<SkyboxTimeSpread> skyboxTimes;
    [SerializeField] private float skyboxBlendThreshold = 0.1f;

    private float sunEulerAngles;
    private float timePassed;
    
    private void Start()
    {
        GameManager.Instance.TimeUpdatedEvent.AddListener(OnTimeUpdated);
        GameManager.Instance.OnDayUpdatedEvent.AddListener(OnDayStart);
    }

    private void OnDayStart(int day)
    {
        sunLight.transform.localEulerAngles = new Vector3(0, 0, 0);
        timePassed = 0;
    }

    private void OnTimeUpdated(int timeRemaining, int timeTotal)
    {
        return;
        
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
       
        
    }

    private void Update()
    {
        if (GameManager.Instance.GetTimeState != GameManager.TimeState.DayActive) return;
        float totalSpinAngle = ((float)GameManager.Instance.gameData.DAY_END_HOUR- GameManager.Instance.gameData.DAY_START_HOUR)/GameManager.Instance.gameData.DAY_END_HOUR * 360;
        float startSpinAngle = (float)GameManager.Instance.gameData.DAY_START_HOUR / 24 * 360;
        float spinAnglePerSecond = totalSpinAngle / GameManager.Instance.gameData.DAY_LENGTH_SECONDS;
        
        
        timePassed += Time.deltaTime;
        float angleProgress = timePassed * spinAnglePerSecond;
        float nextAngle = angleProgress + (startSpinAngle - 90);
        float currentNormalizedProgess = nextAngle / totalSpinAngle;
        float currentNormalizedAngleProgess = angleProgress / totalSpinAngle;
        
        SkyboxTimeSpread currentMaterial = null;
        SkyboxTimeSpread nextMaterial = null;
        for (int i = 0; i < skyboxTimes.Count; i++)
        {
            if (currentNormalizedAngleProgess > skyboxTimes[i].normalizedStartTime &&
                currentNormalizedAngleProgess <= skyboxTimes[i].normalizedEndTime)
            {
                currentMaterial = skyboxTimes[i];
                nextMaterial = skyboxTimes[(i + 1) % skyboxTimes.Count];
                float loopedNextStartTime = nextMaterial.normalizedStartTime;
                if (loopedNextStartTime <= 0)
                {
                    loopedNextStartTime = 1.0f - loopedNextStartTime;
                }

                if (currentNormalizedAngleProgess > loopedNextStartTime - skyboxBlendThreshold)
                {
                    float blendAmount = (loopedNextStartTime - currentNormalizedAngleProgess)/skyboxBlendThreshold;
                    skyboxMaterial.Lerp(currentMaterial.skyboxMaterial, nextMaterial.skyboxMaterial, 1f-blendAmount);
                }
                else
                {
                    skyboxMaterial.CopyMatchingPropertiesFromMaterial(skyboxTimes[i].skyboxMaterial);
                }
            }
        }
        
        
        sunLight.transform.localEulerAngles = new Vector3(nextAngle, 0, 0);
    }
}

[Serializable]
public class SkyboxTimeSpread
{
    public float normalizedStartTime;
    public float normalizedEndTime;
    public Material skyboxMaterial;
}
