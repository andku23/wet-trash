using Unity.Netcode;

public class NetworkBoatState : NetworkBehaviour
{
    public NetworkVariable<float> MoveSpeed = new NetworkVariable<float>(1);
    public NetworkVariable<float> TurnSpeed = new NetworkVariable<float>(1);
    public NetworkVariable<bool> HasDriver = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> DriverID = new NetworkVariable<ulong>(0);
}
