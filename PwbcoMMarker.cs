using UnityEngine;

namespace PWBFuelBalancer
{
  public class PwbcoMMarker : MonoBehaviour
  {
    private ModulePWBFuelBalancer _linkedPart;

    public void LinkPart(ModulePWBFuelBalancer newPart)
    {
      //print("Linking part");
      _linkedPart = newPart;
    }

    private void LateUpdate()
    {
      if (null == _linkedPart) return;
      transform.position = _linkedPart.vessel.findWorldCenterOfMass();
      transform.rotation = _linkedPart.vessel.transform.rotation;

      // print("Actual CoM marker position has been set to: " + transform.position);
    }
  }

}
