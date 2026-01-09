
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable Objects/GameData")]
public class GameData : ScriptableObject
{
    public int DAY_LENGTH_SECONDS = 300;
    public int DAY_START_HOUR = 6;
    public int DAY_END_HOUR = 24;
    public float QUOTA_PERCENTAGE = 0.5f; // percnetage of total loot price spawned required for quota
    //public int INITIAL_CASH = 400;
    public CurrencyValuePair[] INITIAL_CASH;
    public float WEIGHT_MULTIPLIER = 1;
    public int NUM_ROGUE_CARDS = 2;
    public int MAX_MONSTERS_PER_DAY = 40;
    public int MONSTER_SPAWN_PER_HOUR = 4;
    public int MONSTER_SPAWN_INITIAL = 10;
    public int LOOT_PROBABILITY_DIVISOR = 400;
    public int NUM_HARVESTABLES = 20;
    public int NUM_OF_LOOT_GROUPS_PER_HOTSPOT = 10;
    public float MONSTER_DETECTION_RANGE_MULTIPLIER = 1;
    public float MONSTER_DAMAGE_MULTIPLIER = 1;
    public int NUM_DOORS = 4;

}

