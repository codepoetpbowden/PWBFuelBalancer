using UnityEngine;

namespace PWBFuelBalancer
{
  // Behaviour to make the MarkerCam move with the main camera - I suspect that I do not need this TODO try to remove
  public class MarkerCamBehaviour : MonoBehaviour
  {
    private void LateUpdate()
    {
      gameObject.transform.position = FlightCamera.fetch.cameras[0].gameObject.transform.position;
      gameObject.transform.rotation = FlightCamera.fetch.cameras[0].gameObject.transform.rotation;
      // print("Setting markercam to be at: " + this.gameObject.transform.position + " and rotation " + Camera.main.transform.position);
    }
  }
}
