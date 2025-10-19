using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [Header("Data Values")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;
    
    [Tooltip("Max Swimming Speed in m/s")]
    public float MaxSwimmingSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;
    
    [Tooltip("Sprint Swim speed of the character in m/s")]
    public float SprintSwimSpeed = 4.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;
    
    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;
    public float WaterSurfaceJumpHeight = 10.0f;
    
    public float BreathFullAmount;
    public float BreathDepleteRate;
    public float BreathReplenishRate;
    
    
    [Header("Dynamic Continuous Values")]
    
    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    public bool VehicleParented = false;
    public bool InWater = false;
    public bool IsDriving = false;
    public bool IsDead = false;
    public bool InWaterOnSurface = false;
}
