using System.Collections;
using UnityEngine;

namespace PWBFuelBalancer
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class InFlightMarkerCam : MonoBehaviour
  {
    private GameObject _markerCamObject;

    private void Awake()
    {
      //print("InFlightMarkerCam::Awake");
      _markerCamObject = null;
    }

    public void Start()
    {
      //print("InFlightMarkerCam::Start");
      CreateMarkerCam();
    }


    private void DestroyMarkerCam()
    {
      // print("InFlightMarkerCam::DestroyMarkerCam");
      if (null == _markerCamObject) return;
      // print("Shutting down the inflight MarkerCamObject");
      // There should be only one, but lets do all of them just in case.
      IEnumerator mcbs = _markerCamObject.GetComponents<MarkerCamBehaviour>().GetEnumerator();
      while (mcbs.MoveNext())
      {
        if (mcbs.Current == null) continue;
        Destroy((MarkerCamBehaviour)mcbs.Current);
      }

      Destroy(_markerCamObject);

      _markerCamObject = null;
    }

    private void CreateMarkerCam()
    {
      if (null != _markerCamObject) return;
      // print("Setting up the inflight MarkerCamObject");
      _markerCamObject = new GameObject("MarkerCamObject");
      _markerCamObject.transform.parent = FlightCamera.fetch.cameras[0].gameObject.transform;//Camera.mainCamera.gameObject.transform; // Set the new camera to be a child of the main camera  
      Camera markerCam = _markerCamObject.AddComponent<Camera>();

      // Change a few things - the depth needs to be higher so it renders over the top
      markerCam.name = "markerCam";
      markerCam.depth = Camera.main.depth + 10;
      markerCam.clearFlags = CameraClearFlags.Depth;
      // Add a behaviour so we can get the MarkerCam to come around and change itself when the main camera changes
      _markerCamObject.AddComponent<MarkerCamBehaviour>(); // TODO can this be removed?

      // Set the culling mask. 
      markerCam.cullingMask = 1 << 17;
    }

    private void OnDestroy()
    {
      DestroyMarkerCam();
    }
  }

}
