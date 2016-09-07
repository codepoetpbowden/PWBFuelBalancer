using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;

namespace PWBFuelBalancer
{
  [KSPAddon(KSPAddon.Startup.EveryScene, false)]
  public class PwbFuelBalancerAddon : MonoBehaviour
  {
    // The Addon on keeps a reference to all the PWBFuelBalancers in the current vessel. If the current vessel changes or is modified then this list will need to be rebuilt.
    private List<ModulePWBFuelBalancer> _listFuelBalancers;

    private static Rect _windowPositionEditor = new Rect(265, 90, 360, 480);
    private static Rect _windowPositionFlight = new Rect(150, 50, 360, 480);
    private static Rect _currentWindowPosition;
    private static GUIStyle _windowStyle;
    private bool _weLockedInputs;

    private ApplicationLauncherButton _stockToolbarButton; // Stock Toolbar Button

    private bool _visable;

    private int _editorPartCount; 

    private int _selectedBalancer;

    public static PwbFuelBalancerAddon Instance
    {
      get;
      private set;
    }

    public PwbFuelBalancerAddon()
    {
      if (Instance == null)
      {
        Instance = this;
      }
    }

    public void Awake()
    {
      //Debug.Log("PWBFuelBalancerAddon:Awake");

      // create the list of balancers
      _listFuelBalancers = new List<ModulePWBFuelBalancer>();

      // Set up the stock toolbar
      GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
      GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGuiAppLauncherDestroyed);

    }

    public void Start()
    {
      //Debug.Log("PWBFuelBalancerAddon:Start");
      _currentWindowPosition = HighLogic.LoadedSceneIsEditor ? _windowPositionEditor : _windowPositionFlight;
      _windowStyle = new GUIStyle(HighLogic.Skin.window);

      if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) return;GameEvents.onVesselWasModified.Add(OnVesselWasModified);
      GameEvents.onVesselChange.Add(OnVesselChange);
      GameEvents.onVesselLoaded.Add(OnVesselLoaded);
      GameEvents.onEditorShipModified.Add(OnEditorShipModified);
      GameEvents.onFlightReady.Add(OnFlightReady);
      GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);
    }


    private static void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> action)
    {
      //This handles scene specific window positioning.  Soon I'll add persistence...
      if (action.from == GameScenes.EDITOR) _windowPositionEditor = _currentWindowPosition;
      else _windowPositionFlight = _currentWindowPosition;

      _currentWindowPosition = action.to == GameScenes.EDITOR ? _windowPositionEditor : _windowPositionFlight;
    }

    private void OnGuiAppLauncherReady()
    {
        _stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(OnAppLaunchToggle,
          OnAppLaunchToggle,
          DummyVoid,
          DummyVoid,
          DummyVoid,
          DummyVoid,
          ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
          GameDatabase.Instance.GetTexture("PWBFuelBalancer/Assets/pwbfuelbalancer_icon_off", false));
    }

    private void OnGuiAppLauncherDestroyed()
    {
      if (_stockToolbarButton == null) return;
      ApplicationLauncher.Instance.RemoveModApplication(_stockToolbarButton);
      _stockToolbarButton = null;
    }

    private void OnAppLaunchToggle()
    {
      _stockToolbarButton.SetTexture(!_visable
        ? GameDatabase.Instance.GetTexture("PWBFuelBalancer/Assets/pwbfuelbalancer_icon_on", false)
        : GameDatabase.Instance.GetTexture("PWBFuelBalancer/Assets/pwbfuelbalancer_icon_off", false));

      _visable = !_visable;
    }


    private void DummyVoid() { }

    private void OnGUI()
    {
      if (_visable)
      {
        //Set the GUI Skin
        //GUI.skin = HighLogic.Skin;
        _currentWindowPosition = GUILayout.Window(947695, _currentWindowPosition, OnWindow, "PWB Fuel Balancer", _windowStyle, GUILayout.MinHeight(20), GUILayout.MinWidth(100), GUILayout.ExpandHeight(true));
      }

      GuiUtils.ComboBox.DrawGui();

      // If the mouse is over our window, then lock the rest of the UI
      if (HighLogic.LoadedSceneIsEditor) PreventEditorClickthrough();
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium) PreventInFlightClickthrough();
    }

    private bool MouseIsOverWindow()
    {
      return _visable
             && _currentWindowPosition.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
    }

    //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
    private void PreventEditorClickthrough()
    {
      bool mouseOverWindow = MouseIsOverWindow();
      if (!_weLockedInputs && mouseOverWindow)
      {
        EditorLogic.fetch.Lock(true, true, true, "PWBFuelBalancer_click");
        _weLockedInputs = true;
      }
      if (!_weLockedInputs || mouseOverWindow) return;
      EditorLogic.fetch.Unlock("PWBFuelBalancer_click");
      _weLockedInputs = false;
    }

    private void PreventInFlightClickthrough()
    {
      bool mouseOverWindow = MouseIsOverWindow();
      if (!_weLockedInputs && mouseOverWindow)
      {
        InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS | ControlTypes.MAP, "PWBFuelBalancer_click");
        _weLockedInputs = true;
      }
      if (!_weLockedInputs || mouseOverWindow) return;
      InputLockManager.RemoveControlLock("PWBFuelBalancer_click");
      _weLockedInputs = false;
    }

    private void OnWindow(int windowId)
    {
      try
      {
        Rect rect = new Rect(_currentWindowPosition.width - 20, 4, 16, 16);
        if (GUI.Button(rect, ""))
        {
          OnAppLaunchToggle();
        }
        GUILayout.BeginVertical();
        List<string> strings = new List<string>();

        if (_listFuelBalancers.Count > 0)
        {
          List<ModulePWBFuelBalancer>.Enumerator balancers = _listFuelBalancers.GetEnumerator();
          while (balancers.MoveNext())
          {
            if (balancers.Current == null) continue;
            strings.Add(balancers.Current.BalancerName);// + " position:" + balancer.vecFuelBalancerCoMTarget.ToString());
                                                        //              GUILayout.Label(balancer.name + " position:" + balancer.vecFuelBalancerCoMTarget.ToString());
          }

          _selectedBalancer = GuiUtils.ComboBox.Box(_selectedBalancer, strings.ToArray(), this);


          // It will be useful to have a reference to the selected balancer
          ModulePWBFuelBalancer selBal = _listFuelBalancers[_selectedBalancer];

          // Provide a facility to change the name of the balancer
          {
            string oldName = selBal.BalancerName;
            string newName = GUILayout.TextField(oldName);

            if (oldName != newName)
            {
              selBal.BalancerName = newName;
            }
          }
          GUILayout.BeginHorizontal();

          GUILayout.BeginVertical();
          if (GUILayout.Button("up"))
          {
            selBal.VecFuelBalancerCoMTarget.y += 0.05f;
          }
          if (GUILayout.Button("down"))
          {
            selBal.VecFuelBalancerCoMTarget.y -= 0.05f;
          }
          GUILayout.EndVertical();
          GUILayout.BeginVertical();

          if (GUILayout.Button("forward"))
          {
            selBal.VecFuelBalancerCoMTarget.x += 0.05f;
          }

          if (GUILayout.Button("back"))
          {
            selBal.VecFuelBalancerCoMTarget.x -= 0.05f;
          }

          GUILayout.EndVertical();
          GUILayout.EndHorizontal();

          GUILayout.BeginHorizontal();
          if (GUILayout.Button("left"))
          {
            selBal.VecFuelBalancerCoMTarget.z += 0.05f;
          }
          if (GUILayout.Button("right"))
          {
            selBal.VecFuelBalancerCoMTarget.z -= 0.05f;
          }
          GUILayout.EndHorizontal();

          {
            string toggleText = selBal.MarkerVisible ? "Hide Marker" : "Show Marker";

            if (GUILayout.Button(toggleText))
            {
              selBal.ToggleMarker();
            }
          }


          // Save slot 1
          GUILayout.BeginHorizontal();

          selBal.Save1Name = GUILayout.TextField(selBal.Save1Name);

          if (GUILayout.Button("Load"))
          {
            selBal.VecFuelBalancerCoMTarget = selBal.VecSave1CoMTarget;
          }

          if (GUILayout.Button("Save"))
          {
            selBal.VecSave1CoMTarget = selBal.VecFuelBalancerCoMTarget;
          }
          GUILayout.EndHorizontal();

          // Save slot 2
          GUILayout.BeginHorizontal();
          selBal.Save2Name = GUILayout.TextField(selBal.Save2Name);

          if (GUILayout.Button("Load"))
          {
            selBal.VecFuelBalancerCoMTarget = selBal.VecSave2CoMTarget;
          }

          if (GUILayout.Button("Save"))
          {
            selBal.VecSave2CoMTarget = selBal.VecFuelBalancerCoMTarget;
          }

          GUILayout.EndHorizontal();

        }
        GUILayout.EndVertical();
        GUI.DragWindow();
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    public void Update()
    {
      // Debug.Log("PWBFuelBalancerAddon:Update");
    }

    public void FixedUpdate()
    {
      try
      {
        // Debug.Log("PWBFuelBalancerAddon:FixedUpdate");

        // With the new onEditorShipModified event, this is no longer necessary.

        // If we are in the editor, and there is a ship in the editor, then compare the number of parts to last time we did this. If it has changed then rebuild the CLSVessel
        //if (!HighLogic.LoadedSceneIsEditor) return;
        //int currentPartCount = 0;
        //currentPartCount = null == EditorLogic.RootPart ? 0 : EditorLogic.SortedShipList.Count;

        //if (currentPartCount == _editorPartCount) return;
        ////Debug.Log("Calling RebuildCLSVessel as the part count has changed in the editor");
        //BuildBalancerList();

        //_editorPartCount = currentPartCount;
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    public void OnDestroy()
    {
      //Debug.Log("PWBFuelBalancerAddon::OnDestroy");

      GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
      GameEvents.onVesselChange.Remove(OnVesselChange);
      GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
      GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
      GameEvents.onFlightReady.Remove(OnFlightReady);
      GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);

      // Remove the stock toolbar button
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
      if (_stockToolbarButton != null)
        ApplicationLauncher.Instance.RemoveModApplication(_stockToolbarButton);
    }

    private void BuildBalancerList()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        BuildBalancerList(FlightGlobals.ActiveVessel.parts);
      }
      else if (HighLogic.LoadedSceneIsEditor)
      {
        // If there is no root part in the editor - this ought to mean that there are no parts. Just clear out everything
        if (null == EditorLogic.RootPart)
        {
          _listFuelBalancers.Clear();
        }
        else
        {
          BuildBalancerList(EditorLogic.RootPart.vessel.parts);
        }
      }
    }

    // Builds a list of off the ModulePWBFuelBalancers in the whole of the current vessel.
    private void BuildBalancerList(Vessel v)
    {
      BuildBalancerList(v.parts);
    }

    private void BuildBalancerList(List<Part> partList)
    {
      _listFuelBalancers.Clear();
      List<Part>.Enumerator iParts = partList.GetEnumerator();
      while (iParts.MoveNext())
      {
        if (iParts.Current == null) continue;
        if (iParts.Current.Modules.Contains<ModulePWBFuelBalancer>())
        {
          _listFuelBalancers.AddRange(iParts.Current.Modules.GetModules<ModulePWBFuelBalancer>());
        }
      }
    }

    // This event is fired when the vessel is changed. If this happens we need to rebuild the list of balancers in the vessel.
    private void OnVesselChange(Vessel data)
    {
      //Debug.Log("Calling BuildBalancerList from OnVesselChange");
      BuildBalancerList(data);
    }

    private void OnVesselWasModified(Vessel data)
    {
      //Debug.Log("Calling RebuildCLSVessel from OnVesselWasModified");

      BuildBalancerList(data);
    }

    private void OnFlightReady()
    {
      // Now build the list of balancers
      //Debug.Log("Calling BuildBalancerList from onFlightReady");
      BuildBalancerList();
    }

    private void OnVesselLoaded(Vessel data)
    {
      //Debug.Log("Calling BuildBalancerList from OnVesselLoaded");
      BuildBalancerList();
    }
    private void OnEditorShipModified(ShipConstruct vesselConstruct)
    {
      if (vesselConstruct.Parts.Count == _editorPartCount) return;
      //Debug.Log("Calling BuildBalancerList from OnEditorShipModified");
      BuildBalancerList(vesselConstruct.Parts);
      _editorPartCount = vesselConstruct.parts.Count;
    }
  }
}