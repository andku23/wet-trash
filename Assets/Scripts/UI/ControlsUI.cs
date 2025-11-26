using System;
using System.Collections.Generic;
using UnityEngine;

public class ControlsUI : MonoBehaviour
{
    public static ControlsUI Instance;
    
    [SerializeField] private ControlUIGroup[] groups;
    [SerializeField] private GameObject parent;
    
    private Dictionary<ControlUIGroupType, ControlUIGroup> groupsDict;
    private ControlUIGroup currentGroup;

    private void Start()
    {
        Instance = this;
        
        groupsDict = new Dictionary<ControlUIGroupType, ControlUIGroup>();
        foreach (ControlUIGroup group in groups)
        {
            groupsDict.Add(group.type, group);
        }

        SetControlUIState(ControlUIGroupType.None);
    }

    private void HideAll()
    {
        Transform[] allChildrenTransforms = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildrenTransforms)
        {
            if(child.gameObject != parent)
                child.gameObject.SetActive(false);
        }
    }
    
    public void SetControlUIState(ControlUIGroupType groupType, bool overridePriority = false)
    {
        ControlUIGroup nextGroup = groupsDict[groupType];

        if (currentGroup != null && !overridePriority && nextGroup.priority < currentGroup.priority)
        {
            return;
        }
        HideAll();
        currentGroup = nextGroup;
        foreach (var go in groupsDict[groupType].group)
        {
            go.SetActive(true);
        }
    }
}

[Serializable]
public class ControlUIGroup
{
    public ControlUIGroupType type;
    public GameObject[] group;
    public int priority;
}

public enum ControlUIGroupType
{
    None = 0,
    Default = 1,
    HoldingObject = 2,
    HoldingUsable = 3,
    Building = 4,
    Swimming = 5
}
