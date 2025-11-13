using Unity.Netcode;
using UnityEngine;

public class AttachmentMotor : NetworkBehaviour, IAttachment
{
    public float speedIncrease = 5.0f;
    
    public void OnAttach(NetworkedBoat boat)
    {
        boat.BoatState.MoveSpeed.Value += speedIncrease;
    }

    public void OnDetach(NetworkedBoat boat)
    {
        boat.BoatState.MoveSpeed.Value -= speedIncrease;
    }
}
