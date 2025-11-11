using UnityEngine;

public interface IAttachment
{
  public void OnAttach(NetworkedBoat boat){}
  public void OnDetach(NetworkedBoat boat){}
  
  public GameObject gameObject { get ; }
}
