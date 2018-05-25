using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PWBFuelBalancer
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class InFlightMarkerCam : MonoBehaviour
  {
    private static GameObject _markerCamObject;
    internal static Camera MarkerCam;
    internal  static InFlightMarkerCam Instance;
    private void Awake()
    {
      //print("InFlightMarkerCam::Awake");
      _markerCamObject = null;
    }

    public void Start()
    {
      //print("InFlightMarkerCam::Start");
      CreateMarkerCam();
      GameEvents.onVesselChange.Add(OnVesselChange);
      Instance = this;
    }

    public bool MarkerCamEnabled {
      get { return MarkerCam?.enabled ?? false; }
      set
      {
        if (MarkerCam == null) CreateMarkerCam();
        if (MarkerCam != null) MarkerCam.enabled = value;
      }
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

    internal static void CreateMarkerCam()
    {
      if (null != _markerCamObject) return;
      // print("Setting up the inflight MarkerCamObject");
      _markerCamObject = new GameObject("MarkerCamObject");
      _markerCamObject.transform.parent = FlightCamera.fetch.cameras[0].gameObject.transform;//Camera.mainCamera.gameObject.transform; // Set the new camera to be a child of the main camera  
      MarkerCam = _markerCamObject.AddComponent<Camera>();

      // Change a few things - the depth needs to be higher so it renders over the top
      MarkerCam.name = "markerCam";
      //MarkerCam.depth = Camera.main.depth - 1;
      MarkerCam.clearFlags = CameraClearFlags.Depth;
      // Add a behaviour so we can get the MarkerCam to come around and change itself when the main camera changes
      _markerCamObject.AddComponent<MarkerCamBehaviour>(); // TODO can this be removed?

      // Set the culling mask. 
      MarkerCam.cullingMask = 1 << 17;
    }

    internal static Camera GetMarkerCam()
    {
      IEnumerator cams = Camera.allCameras.GetEnumerator();
      while (cams.MoveNext())
      {
        if (cams.Current == null) continue;
        if (((Camera)cams.Current).name == "markerCam")
        {
          return ((Camera)cams.Current);
        }
      }
      return null;
    }

    private void OnVesselChange(Vessel data)
    {
      //Debug.Log("Setting MarkerCam.enabled from OnVesselChange");
      MarkerCamEnabled = !data.isEVA && IsMarkerCamEnabled(data.parts);
    }

    internal static bool IsMarkerCamEnabled(List<Part> parts)
    {
      List<ModulePWBFuelBalancer> balancerList = PwbFuelBalancerAddon.GetBalancers(parts);
      if (balancerList.Count == 0) return false;
      bool markerVisible = false;
      List<ModulePWBFuelBalancer>.Enumerator iList = balancerList.GetEnumerator();
      while (iList.MoveNext())
      {
        if (iList.Current == null) continue;
        if (!iList.Current.MarkerVisible) continue;
        markerVisible = true;
        break;
      }
      return !MapView.MapIsEnabled && markerVisible;
    }

    private void OnDestroy()
    {
      DestroyMarkerCam();
      GameEvents.onVesselChange.Remove(OnVesselChange);
    }
  }

}
