using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable Objects/GameData")]
public class GameData : ScriptableObject
{
    public int DAY_LENGTH_SECONDS = 300;
    public int DAY_START_HOUR = 6;
    public int DAY_END_HOUR = 24;
    public float QUOTA_PERCENTAGE = 0.5f; // percnetage of total loot price spawned required for quota
    public int INITIAL_CASH = 400;
    public float WEIGHT_MULTIPLIER = 1;
    public int NUM_ROGUE_CARDS = 2;
}
