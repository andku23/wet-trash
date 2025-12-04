using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable Objects/GameData")]
public class GameData : ScriptableObject
{
    public int DAY_LENGTH_SECONDS = 300;
    public int DAY_START_HOUR = 6;
    public int DAY_END_HOUR = 24;
    public int INCREMENT_QUOTA;
}
