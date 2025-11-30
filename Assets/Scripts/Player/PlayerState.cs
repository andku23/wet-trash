using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerState : NetworkBehaviour
{
    [Header("Network Variables")]
    public NetworkVariable<float> Health = new NetworkVariable<float>(10);
    
    [Header("Land Data Values")]
    [Tooltip("Move speed of the character in m/s")]
    public float LandMoveSpeed = 2.0f;
    
    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;
    
    [Tooltip("Multiplier based on held item")]
    public float LandWeightMultiplier = 0.01f;
    
    [Space(5)]
    [Header("Swim Data Values")]
    [Tooltip("Swim speed of the character in m/s")]
    public float SwimMoveSpeed = 2.0f;
    
    [Tooltip("Sprint Swim speed of the character in m/s")]
    public float SprintSwimSpeed = 4.335f;
    
    [Tooltip("Initial speed when sprinting while swiming m/s")]
    public float SprintLaunchSwimSpeed = 6.335f;
    
    [Tooltip("How long the initial spring lasts")]
    public float SprintLaunchSwimTime = 2.0f;
    
    [Tooltip("Initial sprint speed curve, 1 is SprintLaunchSwimSpeed, 0 is SprintSwimSpeed")]
    public AnimationCurve SprintLaunchSwimCurve;
    
    [Tooltip("Vertical Swim speed")]
    public float WaterVerticalSwimSpeed = 4.0f;
    
    [Tooltip("Multiplier based on held item")]
    public float SwimWeightMultiplier = 0.01f;

    [Space(5)]
    [Header("Lerping Smoothness")]
    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    [Range(0.0f, 20.0f)]
    public float SpeedChangeRate = 10.0f;
    
    [Space(5)]
    [Header("Player Upgrade Data")]
    public float SprintSpeed_UpgradeIncrement = 0.5f;
    public float SprintSwimSpeed_UpgradeIncrement = 0.5f;
    
    [Space(5)]
    [Header("Jumping")]
    public float JumpHeight = 1.2f;
    public float WaterSurfaceJumpHeight = 10.0f;
    
    [Space(5)]
    [Header("Breath")]
    public float BreathFullAmount;
    public float BreathFullAmount_UpgradeIncrement = 1f;
    public float BreathDepleteRate;
    public float BreathReplenishRate;

    public float MAX_HEALTH = 10;
    public float MAX_INTERACTION_DISTANCE = 5.0f;
    
    
    [Header("Dynamic Runtime Values")]
    public bool Grounded = true;
    public bool VehicleParented = false;
    public bool InWater = false;
    public bool IsDriving = false;
    public bool IsDead = false;
    public bool InWaterOnSurface = false;
    public float WeightCarried = 0f;
    
}
