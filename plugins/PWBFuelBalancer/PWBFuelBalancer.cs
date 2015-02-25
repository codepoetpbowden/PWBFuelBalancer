using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;



[KSPAddon(KSPAddon.Startup.EveryScene, false)]
public class PWBFuelBalancerAddon : MonoBehaviour
{
    // The Addon on keeps a reference to all the PWBFuelBalancers in the current vessel. If the current vessel changes or is modified then this list will need to be rebuilt.
    List<ModulePWBFuelBalancer> listFuelBalancers;

    private static Rect windowPosition = new Rect(0, 0, 360, 480);
    private static GUIStyle windowStyle = null;
    private bool weLockedInputs = false;

    private ApplicationLauncherButton stockToolbarButton = null; // Stock Toolbar Button

    private bool visable = false;

    private int editorPartCount = 0; // This is horrible. Because there does not seem to be an obvious callback to sink when parts are added and removed in the editor, on each fixed update we will could the parts and if it has changed then rebuild the balancer list. Yuk!

    private int selectedBalancer = 0;

    public static PWBFuelBalancerAddon Instance
    {
        get;
        private set;
    }

    public PWBFuelBalancerAddon()
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
        this.listFuelBalancers = new List<ModulePWBFuelBalancer>();

        // Set up the stock toolbar
        GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
        GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);

    }

    public void Start()
    {
        Debug.Log("PWBFuelBalancerAddon:Start");

        windowStyle = new GUIStyle(HighLogic.Skin.window);

        try
        {
            RenderingManager.RemoveFromPostDrawQueue(0, OnDraw);
        }
        catch
        {
            // This is generally not a problem - do not log it.
            // Debug.LogException(ex);
        }

        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
        {
            RenderingManager.AddToPostDrawQueue(0, OnDraw);

            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselLoaded.Add(OnVesselLoaded);
            GameEvents.onFlightReady.Add(OnFlightReady);
        }
                
    }

    void OnGUIAppLauncherReady()
    {
        if (ApplicationLauncher.Ready)
        {
            this.stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(onAppLaunchToggleOn,
                                                                                     onAppLaunchToggleOff,
                                                                                     DummyVoid,
                                                                                     DummyVoid,
                                                                                     DummyVoid,
                                                                                     DummyVoid,
                                                                                     ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT,
                                                                                     (Texture)GameDatabase.Instance.GetTexture("PWBFuelBalancer/Assets/pwbfuelbalancer_icon_off", false));
        }
    }

    void OnGUIAppLauncherDestroyed()
    {
        if (this.stockToolbarButton != null)
        {
            ApplicationLauncher.Instance.RemoveModApplication(this.stockToolbarButton);
            this.stockToolbarButton = null;
        }
    }

    void onAppLaunchToggleOn()
    {
        this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("PWBFuelBalancer/Assets/pwbfuelbalancer_icon_on", false));
        this.visable = true;
    }

    void onAppLaunchToggleOff()
    {
        this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("PWBFuelBalancer/Assets/pwbfuelbalancer_icon_off", false));

        this.visable = false;
    }

    void DummyVoid() { }

    private void OnDraw()
    {
        if (this.visable)
        {
            //Set the GUI Skin
            //GUI.skin = HighLogic.Skin;

            windowPosition = GUILayout.Window(947695, windowPosition, OnWindow, "PWB Fuel Balancer", windowStyle, GUILayout.MinHeight(20), GUILayout.ExpandHeight(true));
        }
    }

    private void OnGUI()
    {
        GuiUtils.ComboBox.DrawGUI();

        // If the mouse if over our window, then lock the rest of the UI
        if (HighLogic.LoadedSceneIsEditor) PreventEditorClickthrough();
        if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium) PreventInFlightClickthrough();
    }

    private bool MouseIsOverWindow()
    {
        if (this.visable
            && PWBFuelBalancerAddon.windowPosition.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y) ))
        {
            return true;
        }
        return false;
    }

    //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
    void PreventEditorClickthrough()
    {
        bool mouseOverWindow = MouseIsOverWindow();
        if (!weLockedInputs && mouseOverWindow)
        {
            EditorLogic.fetch.Lock(true, true, true, "PWBFuelBalancer_click");
            weLockedInputs = true;
        }
        if (weLockedInputs && !mouseOverWindow)
        {
            EditorLogic.fetch.Unlock("PWBFuelBalancer_click");
            weLockedInputs = false;
        }
    }

    void PreventInFlightClickthrough()
    {
        bool mouseOverWindow = MouseIsOverWindow();
        if (!weLockedInputs && mouseOverWindow)
        {
            InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS | ControlTypes.MAP, "PWBFuelBalancer_click");
            weLockedInputs = true;
        }
        if (weLockedInputs && !mouseOverWindow)
        {
            InputLockManager.RemoveControlLock("PWBFuelBalancer_click");
            weLockedInputs = false;
        }
    }

    private void OnWindow(int windowID)
    {
        try
        {
            if (this.listFuelBalancers.Count > 0)
            {
                GUILayout.BeginVertical();
                List<String> strings = new List<String>();


                foreach (ModulePWBFuelBalancer balancer in this.listFuelBalancers)
                {
                    strings.Add(balancer.balancerName + " position:" + balancer.vecFuelBalancerCoMTarget.ToString());
                    //              GUILayout.Label(balancer.name + " position:" + balancer.vecFuelBalancerCoMTarget.ToString());
                }

                this.selectedBalancer = GuiUtils.ComboBox.Box(this.selectedBalancer, strings.ToArray(), this);

                // Provide a facility to change the name of the balancer
                {
                    String oldName = this.listFuelBalancers[this.selectedBalancer].balancerName;
                    String newName = GUILayout.TextField(oldName);

                    if (oldName != newName)
                    {
                        this.listFuelBalancers[this.selectedBalancer].balancerName = newName;
                    }
                }
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                if (GUILayout.Button("up"))
                {
                    this.listFuelBalancers[this.selectedBalancer].vecFuelBalancerCoMTarget.y += 0.05f;
                }
                if (GUILayout.Button("down"))
                {
                    this.listFuelBalancers[this.selectedBalancer].vecFuelBalancerCoMTarget.y -= 0.05f;
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();

                if (GUILayout.Button("forward"))
                {
                    this.listFuelBalancers[this.selectedBalancer].vecFuelBalancerCoMTarget.x += 0.05f;
                }

                if (GUILayout.Button("back"))
                {
                    this.listFuelBalancers[this.selectedBalancer].vecFuelBalancerCoMTarget.x -= 0.05f;
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("left"))
                {
                    this.listFuelBalancers[this.selectedBalancer].vecFuelBalancerCoMTarget.z += 0.05f;
                }
                if (GUILayout.Button("right"))
                {
                    this.listFuelBalancers[this.selectedBalancer].vecFuelBalancerCoMTarget.z -= 0.05f;
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
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

 
            // If we are in the editor, and there is a ship in the editor, then compare the number of parts to last time we did this. If it has changed then rebuild the CLSVessel
            if (HighLogic.LoadedSceneIsEditor)
            {
                int currentPartCount = 0;
                if (null == EditorLogic.RootPart)
                {
                    currentPartCount = 0; // I know that this is already 0, but just to make the point - if there is no startPod in the editor, then there are no parts in the vessel.
                }
                else
                {
                    currentPartCount = EditorLogic.SortedShipList.Count;
                }

                if (currentPartCount != this.editorPartCount)
                {
                    //Debug.Log("Calling RebuildCLSVessel as the part count has changed in the editor");
                    this.BuildBalancerList();

                    this.editorPartCount = currentPartCount;
                }
            }
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
        GameEvents.onFlightReady.Remove(OnFlightReady);

        // Remove the stock toolbar button
        GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
        if (this.stockToolbarButton != null)
            ApplicationLauncher.Instance.RemoveModApplication(stockToolbarButton);
    }

    private void BuildBalancerList()
    {
        if (HighLogic.LoadedSceneIsFlight)
        {
            BuildBalancerList(FlightGlobals.ActiveVessel);
        }
        else if (HighLogic.LoadedSceneIsEditor)
        {
            if (null == EditorLogic.RootPart)
            {
                // There is no root part in the editor - this ought to mean that there are no parts. Just clear out everything
                listFuelBalancers.Clear();
            }
            else
            {
                BuildBalancerList(EditorLogic.RootPart);
            }
        }
    }

    // Builds a list of off the ModulePWBFuelBalancers in the whole of the current vessel.
    private void BuildBalancerList(Vessel v)
    {
        BuildBalancerList(v.rootPart);
    }
    
    // Builds a list of off the ModulePWBFuelBalancers in the whole of the current vessel.
    private void BuildBalancerList(Part rootPart)
    {
        Debug.Log("PWBFuelBalancerAddon::BuildBalancerList");
        // Clear out the current list of balancers
        this.listFuelBalancers.Clear();

        // Build a new list
        ProcessPart(rootPart);

        Debug.Log("Count of new list of Balancers: " + listFuelBalancers.Count);
    }

    private void ProcessPart(Part p)
    {
        Debug.Log("PWBFuelBalancerAddon::ProcessPart");

        foreach (ModulePWBFuelBalancer balancer in p.Modules.OfType<ModulePWBFuelBalancer>())
        {
            this.listFuelBalancers.Add(balancer);
        }

        foreach (Part child in p.children)
        {
            ProcessPart(child);
        }
    }

    // This event is fired when the vessel is changed. If this happens we need to rebuild the list of balancers in the vessel.
    private void OnVesselChange(Vessel data)
    {
        //Debug.Log("Calling BuildBalancerList from OnVesselChange");
        this.BuildBalancerList(data);
    }

    private void OnVesselWasModified(Vessel data)
    {
        //Debug.Log("Calling RebuildCLSVessel from OnVesselWasModified");

        this.BuildBalancerList(data);
    }

    private void OnFlightReady()
    {
        // Now build the list of balancers
        //Debug.Log("Calling BuildBalancerList from onFlightReady");
        this.BuildBalancerList();
    }

    private void OnVesselLoaded(Vessel data)
    {
        //Debug.Log("Calling BuildBalancerList from OnVesselLoaded");
        this.BuildBalancerList();
    }
}


class PartAndResource
{
    public Part part;
    public PartResource resource;

    public PartAndResource(Part pPart, PartResource pResource)
    {
        this.part = pPart;
        this.resource = pResource;
    }
}

public class PWBKSPFuelBalancer : ModulePWBFuelBalancer
{ // This is required as a saved game will have the old Module name in the save file. 
}
  
public class ModulePWBFuelBalancer : PartModule
{

    
    System.Collections.ArrayList tanks;
    int iNextSourceTank;
    int iNextDestinationTank;
    float fNextAmountMoved;
    float fMostMovedThisRound;
    float fStartingMoveAmount;
    private OSD osd;
    public  GameObject SavedCoMMarker;
    public GameObject ActualCoMMarker;
    private bool markerVisible;
    private bool started = false; // used to tell if we are set up and good to go. The Update method will check this know if it is a good idea to try to go anything or not.
    DateTime lastKeyInputTime;

    [KSPField]
    public string setMassKey = "m";
    [KSPField]
    public string displayMarker = "d";
    
    [KSPField(isPersistant = true)]
    public UnityEngine.Vector3 vecFuelBalancerCoMTarget;

    [KSPField(isPersistant = true)]
    public String balancerName = "PWBFuelBalancer";

    [KSPField(isPersistant = true)]
    public UnityEngine.Quaternion rotationInEditor;

    [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
    public string Status;

    [KSPField(isPersistant = false, guiActive = true, guiName = "CoM Error", guiUnits="m" , guiFormat="f3")]
    public float fComError;

    [KSPAction("Balance Fuel Tanks")]
    public void BalanceFuelAction(KSPActionParam param) 
    { 
        BalanceFuel(); 
    }

    [KSPEvent(guiActive = true, guiName = "Deactivate", active = false)]
    public void Disable()
    {
        this.Status = "Deactivated";
        Events["Disable"].active = false;
        Events["BalanceFuel"].active = true;
        Events["Maintain"].active = true;
        
        // Clear the list of tanks. They will have to be rebuilt next time balancing is enabled
        this.tanks = null;
    }

    [KSPEvent(guiActive = true, guiName = "Keep Balanced", active = true)]
    public void Maintain()
    {
        // If we were previously Deactivated then we need to build a list of tanks and set up to start balancing
        if (Status == "Deactivated" || Status == "Balance not possible")
        {
            BuildTanksList();

            this.iNextSourceTank=0;
            this.iNextDestinationTank=0;
            this.fNextAmountMoved = this.fStartingMoveAmount;
            this.fMostMovedThisRound=0;
        }
        
        Events["Disable"].active = true;
        Events["BalanceFuel"].active = false;
        Events["Maintain"].active = false;
        Status = "Maintaining";
        
        return;
    }
    
    [KSPEvent(guiActive = true, guiName = "Balance Fuel", active = true)]
    public void BalanceFuel()
    {
        // If we were previousyl deactive, then we need to build the loist of tanks and set up to start balancing
        if (Status == "Deactivated" || Status == "Balance not possible")
        {
            BuildTanksList();

            this.iNextSourceTank=0;
            this.iNextDestinationTank=0;
            this.fNextAmountMoved = this.fStartingMoveAmount;
            this.fMostMovedThisRound=0;
        }
        
        Events["Disable"].active = true;
        Events["BalanceFuel"].active = false;
        Events["Maintain"].active = false;
        Status = "Balancing";

        return;
    }

    private void BuildTanksList()
    {
        // Go through all the parts and get a list of the tanks which should save us some bother
        //print("building a tank list");

        this.tanks = new System.Collections.ArrayList();
        foreach (Part _part in this.vessel.Parts)
        {
            // Step over all resources in this tank.
            foreach (PartResource _resource in _part.Resources)
            {
                if (_resource.info.density > 0)
                { // Only consider resources that have mass (don't move electricity!)
                    tanks.Add(new PartAndResource(_part, _resource));
                }
            }
        }

        return;
    }

    public void OnDestroy()
    {
        this.started = false;
        DestroySavedComMarker();
    }



    /// <summary>
    /// Constructor style setup.
    /// Called in the Part\'s Awake method. 
    /// The model may not be built by this point.
    /// </summary>
    public override void OnAwake()
    {
        //print("PWBKSPFueBalancer::OnAwake");
        tanks = null;
        markerVisible = false;
    }

    /// <summary>
    /// Called during the Part startup.
    /// StartState gives flag values of initial state
    /// </summary>
    public override void OnStart(StartState state)
    {
        print("PWBFueBalancer::OnStart");
        // Set the status to be deactivated
        Status = "Deactivated";
        osd = new OSD();
        fStartingMoveAmount = 1; // TODO change this to reflect flow rates and the physics frame rate
        SavedCoMMarker = null; // marker to display the saved location

        this.lastKeyInputTime = DateTime.Now;

        this.CreateSavedComMarker();

        this.started = true;
    }

    /// <summary>
    /// Per-frame update
    /// Called ONLY when Part is ACTIVE!
    /// </summary>
    public override void OnUpdate()
    {
    }

    /// <summary>
    /// Per-physx-frame update
    /// </summary>
    private void FixedUpdate()
    {
        if (this.started)
        {
            // Only do this while in flight
            if(HighLogic.LoadedSceneIsFlight)
            {
                // Update the ComError (hopefully this will not kill our performance)
                this.fComError = this.CalculateCoMFromTargetCoM(part.vessel.findWorldCenterOfMass());

                if (this.Status != "Deactivated" && this.Status != "Balance not possible")
                {
                    if (fComError < 0.002)
                    {
                        // The error is so small we need not worry anymore
                        if (Status == "Balancing")
                        {
                            this.Status = "Deactivated";
                            Events["Disable"].active = false;
                            Events["BalanceFuel"].active = true;
                            Events["Maintain"].active = true;

                            // Clear the list of tanks. They will have to be rebuilt next time balancing is enabled
                            this.tanks = null;
                        }
                        else if (Status == "Maintaining")
                        {
                            // Move from a maintaining state to a standby one. If the error increases we con mvoe back to a maintining state
                            this.Status = "Standby";

                            this.iNextSourceTank = 0;
                            this.iNextDestinationTank = 0;
                            this.fNextAmountMoved = this.fStartingMoveAmount;
                            this.fMostMovedThisRound = 0;
                        }
                    }
                    else
                    {
                        // There is an error
                        if (this.Status == "Standby")
                        {
                            // is the error large enough to get us back into a maintaining mode?
                            if (fComError > 0.002 * 2)
                            {
                                this.Status = "Maintaining";
                            }
                        }
                        this.MoveFuel();
                    }
                }
            }
        }
    }

    private float CalculateCoMFromTargetCoM(Vector3 vecWorldCoM)
    {
        Vector3 vecTargetComRotated = (this.transform.rotation * Quaternion.Inverse(this.rotationInEditor)) * this.vecFuelBalancerCoMTarget;
        Vector3 vecTargetPositionInWorldSpace = this.part.transform.position+ vecTargetComRotated;

        float distanceFromCoMToTarget = (vecTargetPositionInWorldSpace - vecWorldCoM).magnitude;

        // print("Distance between the CoM location and the target CoM location: " + distanceFromCoMToTarget.ToString());
  
        return (distanceFromCoMToTarget);
    }

    /// <summary>
    /// Called when PartModule is asked to save its values.
    /// Can save additional data here.
    /// </summary>
    /// <param name='node'>The node to save in to</param>
    public override void OnSave(ConfigNode node)
    {
        //print("PWBKSPFueBalancer::OnSave");

    }

    /// <summary>
    /// Called when PartModule is asked to load its values.
    /// Can load additional data here.
    /// </summary>
    /// <param name='node'>The node to load from</param>
    public override void OnLoad(ConfigNode node)
    {
        print("PWBKSPFueBalancer::OnLoad");

        // For now just dump out what the Config nodes are...
        dumpConfigNode(node);

        // Is the rotation config value set?
        if (!node.values.Contains("rotationInEditor"))
        {
            // rotationInEditor does not exist - must be a v0.0.3 craft. We need to upgrade it.
            print("rotationInEditor was not set.");
            // Only bother to upgrade if we are in flight. If we are in the VAB/SPH then the user can fix the CoM themselves (or just lauch)
            if (HighLogic.LoadedSceneIsFlight)
            {
                // TODO remove diagnostic
                {
                    // I am suspicious that on loading the vessel rotation is not properly set. Let us check
                    print("In onload this.vessel.transform.rotation" + this.vessel.transform.rotation);
                }
                this.rotationInEditor = this.part.transform.rotation * Quaternion.Inverse(this.vessel.transform.rotation);
                print("rotationInEditor was not set. In flight it has been set to: " + this.rotationInEditor);

            }
            else if(HighLogic.LoadedSceneIsEditor)
            {
                this.rotationInEditor = this.part.transform.rotation * Quaternion.Inverse(EditorLogic.VesselRotation);
                print("rotationInEditor was not set. In the editor it has been set to: " + this.rotationInEditor);

            }

        }
    }

    private void dumpConfigNode(ConfigNode node)
    {
        print("ConfigNode: name: " + node.name.ToString() + " id: " + node.id.ToString());
        print("values: ");
        print("ToString: " + node.ToString());
    }

    public void OnMouseOver()
    {
        if (HighLogic.LoadedSceneIsEditor)
        {
            if (part.isConnected && Input.GetKey(setMassKey))
            {
                if (DateTime.Now > this.lastKeyInputTime.AddMilliseconds(100))
                {
                    this.lastKeyInputTime = DateTime.Now;
                    this.SetCoMTarget();
                }
            }
            else if(part.isConnected && Input.GetKey(displayMarker))
            {
                if (DateTime.Now > this.lastKeyInputTime.AddMilliseconds(100))
                {
                    this.lastKeyInputTime = DateTime.Now;
                    this.ToggleMarker();
                }
            }
        }
    }

    [KSPEvent(guiActive = true, guiName = "Toggle Marker", active = true)]
    public void ToggleMarker()
    {
        this.markerVisible = !this.markerVisible;

        // If we are in mapview then hide the marker
        if(null != this.SavedCoMMarker)
        {
            if (MapView.MapIsEnabled)
            {
                this.SavedCoMMarker.SetActive(false);
            }
            else
            {
                this.SavedCoMMarker.SetActive(this.markerVisible);
            }
        }

        if (null != this.ActualCoMMarker)
        {
            if (MapView.MapIsEnabled)
            {
                this.ActualCoMMarker.SetActive(false);
            }
            else
            {
                this.ActualCoMMarker.SetActive(this.markerVisible);
            }
        }

        // TODO remove - Diagnostics
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                print("vessel.transform.rotation : " + this.vessel.transform.rotation);
                print("vessel.ReferenceTransform.rotation : " + this.vessel.ReferenceTransform.rotation);
                print("vessel.transform.rotation .eulerAngles: " + this.vessel.transform.rotation.eulerAngles);
                print("vessel.upaxis : " + this.vessel.upAxis);

                print("upaxis: " + (Vector3)(Quaternion.Inverse(this.vessel.transform.rotation) *this.vessel.upAxis ));
            }
        }
    }

    private void SetCoMTarget()
    {
        // We are depending on the CoM indicator for the location of the CoM which is a bit rubbish :( There ust be a better way of doing this!
        EditorMarker_CoM CoM = (EditorMarker_CoM)GameObject.FindObjectOfType(typeof(EditorMarker_CoM));
        if (CoM == null)
        {
            // There is no CoM indicator. Spawn an instruction screen or something
            osd.Error("To set the target CoM, first turn on the CoM Marker");
        }
        else
        {
            // get the location of the centre of mass
            //print("Com position: " + CoM.transform.position);
            Vector3 vecCom = CoM.transform.position;
            //print("vecCom: " + vecCom);

            this.rotationInEditor = part.transform.rotation;
            //print("Part position: " + part.transform.position);
            Vector3 vecPartLocation = part.transform.position;
            //print("vecPartLocation: " + vecPartLocation);

            // What really interests us is the location fo the CoM relative to the part that is the balancer 
            this.vecFuelBalancerCoMTarget = vecCom - vecPartLocation;
            //print("vecFuelBalancerCoMTarget: " + this.vecFuelBalancerCoMTarget + "rotationInEditor: " + this.rotationInEditor);

            // Set up the marker if we have not already done this.
            if (null == this.SavedCoMMarker)
            {
                //print("Setting up the CoM marker - this should have happened on Startup!");
                this.CreateSavedComMarker();
            }

            osd.Success("The CoM has been set");

            // TODO remove - Diagnostics
            {
                print("EditorLogic.VesselRotation : " + EditorLogic.VesselRotation);
            }
        }
        //print("Setting the targetCoM location for fuel balancing.");
    }


    private void CreateSavedComMarker()
    {
        
        // Do not try to create the marker is it already exisits
        if (null == this.SavedCoMMarker)
        {
            // First try to find the camera that will be used to display the marker - it needs a special camera to make it "float"
            Camera markerCam = null;
            foreach (Camera _cam in Camera.allCameras)
            {
                if (_cam.name == "markerCam")
                {
                    markerCam = _cam;
                }
            }

            // Did we find the camera? If we did then set up the marker object, and idsplkay it via tha camera we found
            if (null != markerCam)
            {
                // Try to create a game object using our marker mesh
                SavedCoMMarker = (GameObject)GameObject.Instantiate(GameDatabase.Instance.GetModel("PWBFuelBalancer/Assets/PWBTargetComMarker"));

                // Make it a bit smaller - we need to fix the model for this
                SavedCoMMarker.transform.localScale = Vector3.one * 0.5f;

                // Add a behaviour to it to allow us to control it and link it to the part that is marks the saved CoM position for
                SavedCoMMarker.AddComponent<SavedCoM_Marker>();
                // Tell the marker which instance of the PWBFueldBalancingModule it is displaying the set CoM location for (we could have more than one per vessel)
                SavedCoMMarker.GetComponent<SavedCoM_Marker>().LinkPart(this);

                // Start the marker visible if it has been set to be visible, or hidden if it is set to be hidden
                SavedCoMMarker.SetActive(this.markerVisible);

                int layer = (int)(Math.Log(markerCam.cullingMask) / Math.Log(2));
                // print("MarkerCam has cullingMask: " + markerCam.cullingMask + " setting marker to be in layer: " + layer);
                SavedCoMMarker.layer = layer;


                // Do it all again to create a marker for the actual centre of mass (rather than the target) TODO find a way of refactoring this
                if(HighLogic.LoadedSceneIsFlight)
                {
                    // Try to create a game object using our marker mesh
                    ActualCoMMarker = (GameObject)GameObject.Instantiate(GameDatabase.Instance.GetModel("PWBFuelBalancer/Assets/PWBComMarker"));

                    // Make it a bit smaller - we need to fix the model for this
                    ActualCoMMarker.transform.localScale = Vector3.one * 0.45f;
                    ActualCoMMarker.renderer.material.color = Color.yellow;

                    // Add a behaviour to it to allow us to control it and link it to the part that is marks the saved CoM position for
                    ActualCoMMarker.AddComponent<PWBCoM_Marker>();
                    // Tell the marker which instance of the PWBFueldBalancingModule it is displaying the set CoM location for (we could have more than one per vessel)
                    ActualCoMMarker.GetComponent<PWBCoM_Marker>().LinkPart(this);

                    // Start the marker visible if it has been set to be visible, or hidden if it is set to be hidden
                    ActualCoMMarker.SetActive(this.markerVisible);

                    print("MarkerCam has cullingMask: " + markerCam.cullingMask + " setting marker to be in layer: " + layer);
                    ActualCoMMarker.layer = layer;
                }

            }
            else
            {
                // No camera - no point in setting up the object, Perhaps there will be another oppertunity.
                print("Warning - could not find the markerCam, It is probably best not to create the marker object as we do not know which layer to place it in anyway");
            }
        }
    }

    private void DestroySavedComMarker()
    {
        // Destroy the Saved Com Marker if the part is destroyed.
        if (null != this.SavedCoMMarker)
        {
            this.SavedCoMMarker.GetComponent<SavedCoM_Marker>().LinkPart(null);
            GameObject.Destroy(this.SavedCoMMarker);
            this.SavedCoMMarker = null;
        }

        // Destroy the Actual Com Marker if the part is destroyed.
        if (null != this.ActualCoMMarker)
        {
            this.ActualCoMMarker.GetComponent<PWBCoM_Marker>().LinkPart(null);
            GameObject.Destroy(this.ActualCoMMarker);
            this.ActualCoMMarker = null;
        }
    }

    // Transfer fuel to move the center of mass from current position towards target.
    // Returns the new distance the CoM was moved towards its target
    public  float MoveFuel()
    {
        float fCoMStartingError = CalculateCoMFromTargetCoM(vessel.findWorldCenterOfMass());
        float mass = vessel.GetTotalMass(); // Get total mass.
        Vector3 OldWorldCoM = vessel.findWorldCenterOfMass();
        float fOldCoMError = fCoMStartingError;
        bool moveMade=false;
        int iNumberofTanks = tanks.Count;

        //print("Number of tanks " + iNumberofTanks);

        // Now go through the list of parts and resources and consider making transfers
        while (this.iNextSourceTank < iNumberofTanks && false == moveMade)
        {
            //print("Considering moveing fuel from tank" + this.iNextSourceTank);
            PartResource resource1 = ((PartAndResource)tanks[iNextSourceTank]).resource;
            Part part1 = ((PartAndResource)tanks[iNextSourceTank]).part;

            // Only process nonempty tanks.
            if (resource1.amount > 0)
            {
                // Only move resources that have mass (don't move electricity!)
                if (resource1.info.density > 0)
                {
                    // If the two tanks are the same move on.
                    if (this.iNextDestinationTank == this.iNextSourceTank)
                    {
                        iNextDestinationTank++;
                    }

                    while (this.iNextDestinationTank < iNumberofTanks && false == moveMade)
                    {
                        //print("Considering moveing fuel to tank" + this.iNextDestinationTank);

                        PartResource resource2 = ((PartAndResource)tanks[iNextDestinationTank]).resource;
                        Part part2 = ((PartAndResource)tanks[iNextDestinationTank]).part;

                        // Check that the resources are of the same type 
                        if (resource2.resourceName == resource1.resourceName)
                        {
                            // Clamp resource quantity by the amount available in the two tanks.
                            float moveAmount = (float)Math.Min(this.fNextAmountMoved, resource1.amount);
                            moveAmount = (float)Math.Max(moveAmount, -(resource1.maxAmount - resource1.amount));
                            moveAmount = (float)Math.Max(moveAmount, -resource2.amount);
                            moveAmount = (float)Math.Min(moveAmount, resource2.maxAmount - resource2.amount);

                            //print("considering moving " + moveAmount.ToString());
                            if (moveAmount > 0)
                            {
                                // Calculate the new CoM to see if it helped:
                                float fVesselMass = vessel.GetTotalMass();

                                //print("part1.transform.position : " + part1.transform.position.ToString());
                                //print("part2.transform.position : " + part2.transform.position.ToString());

                                Vector3 NewCenterOfMass = ((OldWorldCoM * fVesselMass) - (part1.transform.position * ((float)(moveAmount * resource1.info.density))) + (part2.transform.position * ((float)(moveAmount * resource2.info.density)))) * (1 / fVesselMass);

                                // Recompute the distance between CoM and the TargetCoM
                                float fNewError = CalculateCoMFromTargetCoM(NewCenterOfMass);

                                //print("Old world CoM: " + OldWorldCoM.ToString());
                                //print("New suggested CoM: " + NewCenterOfMass.ToString());
                                //print("new error is: " + fNewError.ToString() + " compared to " + fOldCoMError.ToString());
                                if (fNewError < fOldCoMError)
                                {
                                    //print("CoM moved in correct direction");
                                    // This is moving us in the right direction
                                    fOldCoMError = fNewError;

                                    // Actually move the fuel
                                    resource1.amount -= moveAmount;
                                    resource2.amount += moveAmount;

                                    // set the new CoM for the next iteration
                                    OldWorldCoM = NewCenterOfMass;
                                    moveMade = true;
                                    if (moveAmount > this.fMostMovedThisRound)
                                    {
                                        this.fMostMovedThisRound = moveAmount;
                                    }
                                    // Finally, move on to another source tank, so that the flow out of each source tank appears a bit smoother.
                                    iNextSourceTank++;
                                }
                            }
                        }

                        // Move on the the next destination tank
                        this.iNextDestinationTank++;
                        if (this.iNextDestinationTank == this.iNextSourceTank)
                        {
                            iNextDestinationTank++;
                        }
                    }

                    // If we have reached the end of the list of destination tanks then we need to reset tank list for next time
                    if (this.iNextDestinationTank >= iNumberofTanks)
                    {
                        this.iNextDestinationTank = 0;
                        this.iNextSourceTank++;
                    }
                }
                else 
                {
                    //print("Tank" + this.iNextSourceTank + " contains a zero density resource, moving on to the next source tank");
                    this.iNextSourceTank++;
                }
            }
            else
            {
                //print("Tank" + this.iNextSourceTank + " was empty, moving on to the next source tank");
                this.iNextSourceTank++;
            }
        }
     
        // If we have reached the end of the source tanks then we need to reset the list for next time
        if (this.iNextSourceTank >= iNumberofTanks)
        {
            this.iNextSourceTank = 0;
            // Since we are now starting a new round, the next thing to consider is whether we moved anything this round. If not then we need to consider moving smaller amounts
            if (this.fMostMovedThisRound == 0)
            {
                this.fNextAmountMoved = this.fNextAmountMoved / 2f;
                //print("changing the amount to move to be " + fNextAmountMoved);

                // Finally has the amount move become so small that we need to call it a day?
                if (this.fNextAmountMoved < 0.0005)
                {
                    // Since perfect balance is not possible, we need to move into an appropriate state.If we are trying to maiintain blanace then we will keep trying trying again with larger amounts. If we were trying for a single balance then move to a state that shows it is not possible.
                    if (this.Status == "Maintaining")
                    {
                        this.fNextAmountMoved = this.fStartingMoveAmount;
                        Events["Disable"].active = true;
                    }
                    else
                    {
                        this.Status = "Balance not possible";
                        Events["Disable"].active = true;
                        Events["BalanceFuel"].active = true;
                        Events["Maintain"].active = true;
                        // throw away the tanks list
                        this.tanks = null;
                    }
                }
            }
            this.fMostMovedThisRound = 0;
        }
    
        // Update the member variable that remembers what the error is to display it
        this.fComError = fOldCoMError;

        // Return the amount that the CoM has been corrected
        return (float)(fCoMStartingError - fOldCoMError);
    }
  
    public void OnGUI()
    {
        if (HighLogic.LoadedSceneIsEditor)
        {
            EditorLogic editor = EditorLogic.fetch;
            if (editor == null) return;
            if (editor.editorScreen == EditorScreen.Parts)
            {
                osd.Update();
            }
        }
    }
}

// Utils - Borrowed from KSP Select Root Mod - credit where it is due
public class OSD
{
    private class Message
    {
        public String text;
        public Color color;
        public float hideAt;
    }

    private List<OSD.Message> msgs = new List<OSD.Message>();

    private static GUIStyle CreateStyle(Color color)
    {
        GUIStyle style = new GUIStyle();
        style.stretchWidth = true;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = color;
        return style;
    }

    Predicate<Message> pre = delegate(Message m) { return (Time.time >= m.hideAt); };
    Action<Message> showMesssage = delegate(Message m) { GUILayout.Label(m.text, CreateStyle(m.color)); };

    public void Update()
    {
        if (msgs.Count == 0) return;
        msgs.RemoveAll(pre);
        GUILayout.BeginArea(new Rect(0, Screen.height*0.1f, Screen.width, Screen.height*0.8f), CreateStyle(Color.white));
        msgs.ForEach(showMesssage);
        GUILayout.EndArea();
    }

    public void Error(String text) {
        AddMessage(text, XKCDColors.LightRed);
    }

    public void Success(String text)
    {
        AddMessage(text, XKCDColors.Cerulean);
    }

    public void Info(String text)
    {
        AddMessage(text, XKCDColors.OffWhite);
    }

    public void AddMessage(String text, Color color, float shownFor)
    {
        OSD.Message msg = new OSD.Message();
        msg.text = text;
        msg.color = color;
        msg.hideAt = Time.time + shownFor;
        msgs.Add(msg);
    }

    public void AddMessage(String text, Color color)
    {
        this.AddMessage(text, color, 3);
    }
}

public class SavedCoM_Marker : MonoBehaviour
{
    ModulePWBFuelBalancer _linkedPart;

    public void LinkPart(ModulePWBFuelBalancer newPart)
    {
        print("Linking part");
        _linkedPart = newPart;
    }

    void LateUpdate()
    {
        if (null != _linkedPart)
        {
            Vector3 vecTargetComRotated =  (this._linkedPart.transform.rotation *  Quaternion.Inverse(this._linkedPart.rotationInEditor)) * this._linkedPart.vecFuelBalancerCoMTarget;
            transform.position = this._linkedPart.part.transform.position +  vecTargetComRotated;
            if(HighLogic.LoadedSceneIsFlight)
            {
                transform.rotation = this._linkedPart.vessel.transform.rotation;
            }
            // print("CoM marker position has been set to: " + transform.position);
        }
    }
}

public class PWBCoM_Marker : MonoBehaviour
{
    ModulePWBFuelBalancer _linkedPart;

    public void LinkPart(ModulePWBFuelBalancer newPart)
    {
        print("Linking part");
        _linkedPart = newPart;
    }

    void LateUpdate()
    {
        if (null != _linkedPart)
        {
            transform.position =  this._linkedPart.vessel.findWorldCenterOfMass();
            transform.rotation = this._linkedPart.vessel.transform.rotation;
            
            // print("Actual CoM marker position has been set to: " + transform.position);
        }
    }
}

[KSPAddon(KSPAddon.Startup.Flight, false)]
public class InFlightMarkerCam : MonoBehaviour
{
    GameObject markerCamObject;

    void Awake()
    {
        //print("InFlightMarkerCam::Awake");
        this.markerCamObject = null;
    }

    public void Start()
    {
        //print("InFlightMarkerCam::Start");
        CreateMarkerCam();
    }


    private void DestroyMarkerCam()
    {
        // print("InFlightMarkerCam::DestroyMarkerCam");
        if (null != markerCamObject)
        {
            // print("Shutting down the inflight MarkerCamObject");
            // There should be only one, but lets do all of them just in case.
            foreach (MarkerCam_Behaviour b in markerCamObject.GetComponents<MarkerCam_Behaviour>())
            {
                Destroy(b);
            }

            GameObject.Destroy(this.markerCamObject);

            this.markerCamObject = null;
        }
    }

    private void CreateMarkerCam()
    {
        if (null == this.markerCamObject)
        {
            // print("Setting up the inflight MarkerCamObject");
            markerCamObject = new GameObject("MarkerCamObject");
            markerCamObject.transform.parent = FlightCamera.fetch.cameras[0].gameObject.transform;//Camera.mainCamera.gameObject.transform; // Set the new camera to be a child of the main camera  
            Camera markerCam = (Camera)markerCamObject.AddComponent<Camera>();

            // Change a few things - the depth needs to be higher so it renders over the top
            markerCam.name = "markerCam";
            markerCam.depth = Camera.main.depth + 10;
            markerCam.clearFlags = CameraClearFlags.Depth;
            // Add a behaviour so we can get the MarkerCam to come around and change itself when the main camera changes
            markerCamObject.AddComponent<MarkerCam_Behaviour>(); // TODO can this be removed?

            // Set the culling mask. 
            markerCam.cullingMask = 1 << 17;
        }
        else
        {
            // Check that it is active and stuff
            /*
             * if (false == markerCamObject.activeSelf)
            {
                print("MarkerCam not active");
            }

            if (markerCamObject.camera.cullingMask != 1 << 17)
            {
                print("MarkerCam cull mask is:" + markerCamObject.camera.cullingMask);
            }

            if (false == markerCamObject.camera.enabled)
            {
                print("MarkerCam is not enabled");
            }
            */
        }
    }

    private void OnDestroy()
    {
        DestroyMarkerCam();
    }
}

// Behaviour to make the MarkerCam move with the main camera - I suspect that I do not need this TODO try to remove
public class MarkerCam_Behaviour : MonoBehaviour
{
    void LateUpdate()
    {
        this.gameObject.transform.position = FlightCamera.fetch.cameras[0].gameObject.transform.position;
        this.gameObject.transform.rotation = FlightCamera.fetch.cameras[0].gameObject.transform.rotation;
        // print("Setting markercam to be at: " + this.gameObject.transform.position + " and rotation " + Camera.main.transform.position);
    }
}


public static class GuiUtils
{
    static GUIStyle _yellowOnHover;
    public static GUIStyle yellowOnHover
    {
        get
        {
            if (_yellowOnHover == null)
            {
                _yellowOnHover = new GUIStyle(GUI.skin.label);
                _yellowOnHover.hover.textColor = Color.yellow;
                Texture2D t = new Texture2D(1, 1);
                t.SetPixel(0, 0, new Color(0, 0, 0, 0));
                t.Apply();
                _yellowOnHover.hover.background = t;
            }
            return _yellowOnHover;
        }
    }


    // Code blagged directly out of MechJeb - Credit where it is due!
    public class ComboBox
    {
        // Easy to use combobox class
        // ***** For users *****
        // Call the Box method with the latest selected item, list of text entries
        // and an object identifying who is making the request.
        // The result is the newly selected item.
        // There is currently no way of knowing when a choice has been made

        // Position of the popup
        private static Rect rect;
        // Identifier of the caller of the popup, null if nobody is waiting for a value
        private static object popupOwner = null;
        private static string[] entries;
        private static bool popupActive;
        // Result to be returned to the owner
        private static int selectedItem;
        // Unity identifier of the window, just needs to be unique
        private static int id = GUIUtility.GetControlID(FocusType.Passive);
        // ComboBox GUI Style
        private static GUIStyle style;

        static ComboBox()
        {
            Texture2D background = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            background.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < background.width; x++)
                for (int y = 0; y < background.height; y++)
                {
                    if (x == 0 || x == background.width - 1 || y == 0 || y == background.height - 1)
                        background.SetPixel(x, y, new Color(0, 0, 0, 1));
                    else
                        background.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, 0.95f));
                }

            background.Apply();

            style = new GUIStyle(GUI.skin.window);
            style.normal.background = background;
            style.onNormal.background = background;
            style.border.top = style.border.bottom;
            style.padding.top = style.padding.bottom;
        }

        public static void DrawGUI()
        {
            Debug.Log("popupActive: " + popupActive);

            if (popupOwner == null || rect.height == 0 || !popupActive)
                return;

            // Make sure the rectangle is fully on screen
            rect.x = Math.Max(0, Math.Min(rect.x, Screen.width - rect.width));
            rect.y = Math.Max(0, Math.Min(rect.y, Screen.height - rect.height));

            rect = GUILayout.Window(id, rect, identifier =>
                {
                    selectedItem = GUILayout.SelectionGrid(-1, entries, 1, yellowOnHover);
                    if (GUI.changed)
                        popupActive = false;
                }, "", style);

            //Cancel the popup if we click outside
            if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
                popupOwner = null;
        }

        public static int Box(int selectedItem, string[] entries, object caller)
        {
            // Trivial cases (0-1 items)
            if (entries.Length == 0)
                return 0;
            if (entries.Length == 1)
            {
                GUILayout.Label(entries[0]);
                return 0;
            }

            // A choice has been made, update the return value
            if (popupOwner == caller && !ComboBox.popupActive)
            {
                popupOwner = null;
                selectedItem = ComboBox.selectedItem;
                GUI.changed = true;
            }

            bool guiChanged = GUI.changed;
            if (GUILayout.Button("↓ " + entries[selectedItem] + " ↓"))
            {
                // We will set the changed status when we return from the menu instead
                GUI.changed = guiChanged;
                // Update the global state with the new items
                popupOwner = caller;
                popupActive = true;
                ComboBox.entries = entries;
                // Magic value to force position update during repaint event
                rect = new Rect(0, 0, 0, 0);
            }
            // The GetLastRect method only works during repaint event, but the Button will return false during repaint
            if (Event.current.type == EventType.Repaint && popupOwner == caller && rect.height == 0)
            {
                rect = GUILayoutUtility.GetLastRect();
                // But even worse, I can't find a clean way to convert from relative to absolute coordinates
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                Vector2 clippedMousePos = Event.current.mousePosition;
                rect.x = (rect.x + mousePos.x) - clippedMousePos.x;
                rect.y = (rect.y + mousePos.y) - clippedMousePos.y;
            }

            return selectedItem;
        }
    }
}