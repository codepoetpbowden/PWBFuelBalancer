using System;
using System.Collections.Generic;
using UnityEngine;

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

public class PWBKSPFuelBalancer : PartModule
{
    System.Collections.ArrayList tanks;
    int iNextSourceTank;
    int iNextDestinationTank;
    float fNextAmountMoved;
    float fMostMovedThisRound;

    [KSPField]
    public string setMassKey = "m";
    [KSPField(isPersistant = true)]
    public UnityEngine.Vector3 vecFuelBalancerCoMTarget;

    [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
    public string Status;

    [KSPField(isPersistant = false, guiActive = true, guiName = "Com Error", guiUnits="m" , guiFormat="f3")]
    public float fComError;


    [KSPAction("Balance Fuel Tanks")]
    public void BalanceFuelAction(KSPActionParam param) 
    { 
        BalanceFuel(); 
    }

    [KSPEvent(guiActive = true, guiName = "Balance Fuel", active = true)]
    private void BalanceFuel()
    {
        // TODO, do something to balance the fuel in the tanks
        //print("BalanceFuel()");
        if (Status == "Deactivated")
        {
            Status = "Balancing";

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
            this.iNextSourceTank=0;
            this.iNextDestinationTank=0;
            this.fNextAmountMoved = 1;
            this.fMostMovedThisRound=0;
            //print("built tank list");
        }

        // TODO, Do we need to do something here to tell things that the status has changed?

        return;
    }

    /// <summary>
    /// Constructor style setup.
    /// Called in the Part\'s Awake method. 
    /// The model may not be built by this point.
    /// </summary>
    public override void OnAwake()
    {
        tanks = null;
    }

    /// <summary>
    /// Called during the Part startup.
    /// StartState gives flag values of initial state
    /// </summary>
    public override void OnStart(StartState state)
    {
        // Set the status to be deactivated
        Status = "Deactivated";

        // Add stuff to the log
        //print("Started the PWB KSP Fuel Balancer");

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
    /// Called ONLY when Part is ACTIVE!
    /// </summary>
    public override void OnFixedUpdate()
    {
        // Update the ComError (hopefully this will not kill our performance)
        this.fComError = this.CalculateCoMFromTargetCoM(part.vessel.findWorldCenterOfMass());

        // Only do anything is we are in a Balancing state
        if (this.Status == "Balancing")
        {
            float originalCoMToTargetCoMDistance = fComError;
            if (originalCoMToTargetCoMDistance < 0.0001)
            {
                // Close enough? stop balancing
                //print("Stopping balancing as reduced distance to " + originalCoMToTargetCoMDistance);
                this.Status = "Deactivated";
            }
            else
            {
                //print("Starting distance from Com to TargetCoM: " =originalCoMToTargetCoMDistance);
                this.AttemptToMoveCoM();
            }
        }
    }

    /// <summary>
    /// Tries to move some fuel around
    /// Returns the new distance from the CoMTarget
    /// </summary>
    private void AttemptToMoveCoM()
    {
        print("Attempting to move fuel. iNextSourceTank:" + iNextSourceTank + " iNextDestinationTank:" + iNextDestinationTank + " fNextAmountMoved:" + fNextAmountMoved + " fMostMovedThisRound:" + fNextAmountMoved);

        // TODO reconsider has we disable after the CoM can not longer be improved
        float fImprovement = MoveFuel();
        //print("moved CoM by: " +fImprovement);
    }


    private float CalculateCoMFromTargetCoM(Vector3 vecWorldCoM)
    {
        // Vector3 vecworldCoM = part.vessel.findWorldCenterOfMass(); (now passed in as a parameter
        // Rotate the COM and part by the inverse of the vessel's rotation, which should put everything back in the position it was in the VAB

        Vector3 vecRotatedWorldCom = Quaternion.Inverse(part.vessel.transform.rotation) * vecWorldCoM;
        Vector3 vecRotatedPartPosition = Quaternion.Inverse(part.vessel.transform.rotation) * part.transform.position;

        // Get the position of the CoM relative to the part. That should be comparable to the relative position of the CoM and part that was noted in the VAB.
        Vector3 vecFuelBalancerCoM = vecRotatedWorldCom - vecRotatedPartPosition;

        //print("position of the CoM from the part:");
        //print(vecFuelBalancerCoM);

        // Finally calculate the distance from the location of the CoM and the target location of the CoM - this is what we need to try to minimize by moving the fuel around.
        Vector3 vecCoMtoTargetCoM = vecFuelBalancerCoM - this.vecFuelBalancerCoMTarget;
        //print("Distance between the CoM location and the target CoM location");
        //print(vecCoMtoTargetCoM.magnitude.ToString());

        return (vecCoMtoTargetCoM.magnitude);
    }

    /// <summary>
    /// Called when PartModule is asked to save its values.
    /// Can save additional data here.
    /// </summary>
    /// <param name='node'>The node to save in to</param>
    public override void OnSave(ConfigNode node)
    {
    }

    /// <summary>
    /// Called when PartModule is asked to load its values.
    /// Can load additional data here.
    /// </summary>
    /// <param name='node'>The node to load from</param>
    public override void OnLoad(ConfigNode node)
    {
    }


    public void OnMouseOver()
    {
        if (HighLogic.LoadedSceneIsEditor)
        {
            if (part.isConnected && Input.GetKey(setMassKey))
            {
                // We are depending on the CoM indicator for the locaiton of the CoM which is a bit rubbish :( There ust be a better way of doing this!
                EditorMarker_CoM CoM = (EditorMarker_CoM)GameObject.FindObjectOfType(typeof(EditorMarker_CoM));
                if (CoM == null)
                {
                    // There is no CoM indicator. Perhaps we should spawn an instruction screen or something
                    /* nothing to do */
                    return;
                }
                else
                {
                    //print("CoM info:");
                    //print(CoM.ToString());
                    // get the location of the centre of mass
                    Vector3 vecFuelBalancerCoMTarget = CoM.transform.position;
                    //print(vecFuelBalancerCoMTarget.ToString());

                    // What really interests us is the location fo the CoM relative to the part that is the balancer // TODO I suppose that means that we need to react to it being moved.
                    Vector3 vecFuelBalancerPartLocation = part.transform.position;
                    //print(vecFuelBalancerPartLocation.ToString());

                    this.vecFuelBalancerCoMTarget = vecFuelBalancerCoMTarget - vecFuelBalancerPartLocation;
                    //print(this.vecFuelBalancerCoMTarget.ToString());


                
                } 

                //print("Setting the targetCoM location for fuel balancing.");
            }
        }
    }
    
    // Borrowed this code from somewhere else
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
                this.fNextAmountMoved = this.fNextAmountMoved / 1.5f;
                //print("changing the amount to move to be " + fNextAmountMoved);

                // Finally has the amount move become so small that we need to call it a day?
                if (this.fNextAmountMoved < 0.001)
                {
                    //print("Considering moving very small amounts. Time to call it a day");
                    this.Status = "Deactivated";
                    // throw away the tanks list
                    this.tanks = null;
                }
            }
            this.fMostMovedThisRound = 0;
        }
    
        // Update the member variable that remembers what the error is to display it
        this.fComError = fOldCoMError;

        // Return the amount that the CoM has been corrected
        return (float)(fCoMStartingError - fOldCoMError);
    }












}

