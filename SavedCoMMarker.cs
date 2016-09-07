using UnityEngine;

namespace PWBFuelBalancer
{
  public class SavedCoMMarker : MonoBehaviour
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
      Vector3 vecTargetComRotated = (_linkedPart.transform.rotation * Quaternion.Inverse(_linkedPart.RotationInEditor)) * _linkedPart.VecFuelBalancerCoMTarget;
      transform.position = _linkedPart.part.transform.position + vecTargetComRotated;
      if (HighLogic.LoadedSceneIsFlight)
      {
        transform.rotation = _linkedPart.vessel.transform.rotation;
      }
      // print("CoM marker position has been set to: " + transform.position);
    }
  }

}
