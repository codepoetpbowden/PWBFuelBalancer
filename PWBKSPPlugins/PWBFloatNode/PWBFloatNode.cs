using System;
using System.Collections.Generic;
using UnityEngine;

namespace PWBFloatNode
{
    public class PWBFloatNode : PartModule
    {
        [KSPField]
        public string moveNodeKey = "n";

        /// <summary>
        /// Constructor style setup.
        /// Called in the Part\'s Awake method. 
        /// The model may not be built by this point.
        /// </summary>
        public override void OnAwake()
        {
        }

        /// <summary>
        /// Called during the Part startup.
        /// StartState gives flag values of initial state
        /// </summary>
        public override void OnStart(StartState state)
        {
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
            if (HighLogic.LoadedSceneIsEditor && Input.GetKey(moveNodeKey))
            {
                if (part.isConnected)
                {
                    print("Part is connected");
                    foreach (AttachNode node in  this.part.attachNodes)
                    {
                        print("considering a node: "+node.id);
                        if(AttachNode.NodeType.Stack == node.nodeType)
                        {
                            print("found  astack node");
                            // This is a stack node - it might be top or bottom.
                            // only consider standard anmed top and bottom nodes
                            if (node.id == "bottom" || node.id == "top")

                            // is this node attached? If not then move the attach node
                            if (null == node.attachedPart)
                            {
                                Vector3 normal = node.orientation;
                                normal.Normalize();
                                float maxd = ProcessParts(part, null, normal);

                                print("maxd: " + maxd);

                                // Now that we know how far along the normal the attach node needs to be we can place it
                                if (0 < maxd)
                                {
                                    print("node.position: " + node.position);
                                    node.position = normal * maxd;
                                    print("new node.position: " + node.position);
                                }
                            }
                        }
                    }


                }
            }
        }

        // Calls Process Part on all the children and the parent, if they are surface mounted, but not on the refereing part
        private float ProcessParts(Part _part, Part refferingPart ,Vector3 normal)
        {
            print("Entering ProcessParts");
            float maxd = 0;
            String refferingPartID = null;
            if (refferingPart != null)
            {
                refferingPartID = refferingPart.ConstructID;
            }
            print("refferingPart : " + refferingPartID);
            print("processing the children of: " + _part.ConstructID);

            foreach (Part _childPart in _part.children)
            {
                if (_childPart.ConstructID != refferingPartID) // ensure that the child is not the reffering part
                {
                    print("considering a child part: " + _childPart.ConstructID);
                    AttachNode node = _part.findAttachNodeByPart(_childPart);

                    if (node == null)
                    {
                        print("No attach point - the child part must be surface mounted");
                        float d = ProcessPart(_childPart, _part, normal);
                        if (d > maxd) { maxd = d; }
                    }
                    else
                    {
                        if (AttachNode.NodeType.Stack == node.nodeType && refferingPart == null) // if the part is stack mounted and the reffering part of null then this must be connected to the stack of our wn part.
                        {
                            print("Not considering this part as it is stack mounted to the orginal part.");
                        }
                        else
                        {
                            float d = ProcessPart(_childPart, _part, normal);
                            print("d = " + d);
                            if (d > maxd) { maxd = d; }
                        }
                    }
                }
            } // foreach()

            // Also consider the parent
            if (_part.parent != null)
            {
                print("considering the parent part: " + _part.parent.ConstructID);
                if (_part.parent.ConstructID != refferingPartID)
                {
                    AttachNode node = _part.findAttachNodeByPart(_part.parent);

                    if (node == null)
                    {
                        print("No attach point - the parent part must be surface mounted");
                        float d = ProcessPart(_part.parent, _part, normal);
                        if (d > maxd) { maxd = d; }
                    }
                    else
                    {
                        if (AttachNode.NodeType.Stack == node.nodeType && refferingPart == null) // if the part is stack mounted and the reffering part of null then this must be connected to the stack of our wn part.
                        {
                            print("Not considering this part as it is stack mounted to the orginal part.");
                        }
                        else
                        {
                            float d = ProcessPart(_part.parent, _part, normal);
                            print("d = " + d);
                            if (d > maxd) { maxd = d; }
                        }
                    }
                }
                else
                {
                    print("parent part is the reffering part, so it will not be consdered.");
                }
            }

            print("Leaving ProcessParts, maxd:"+maxd);
            
            return (maxd);
        }

        private float ProcessPart(Part _part, Part refferingPart ,Vector3 normal)
        {
            print("Entering ProcessPart. part:" +_part.name + " constructID: " +_part.ConstructID);
            float maxd = 0;
            // What is the Normal to the plane? 
//            Vector3 normal = part.transform.rotation * Vector3.up;
            Vector3 pointInPlane = part.transform.localToWorldMatrix.MultiplyPoint3x4(Vector3.zero); // use origin as the point in the plane

            print("Normal: " + normal);
            print("pointInPlane: " + pointInPlane);
            // go through all the verticies in the collider mesh of the part and find out the one that is furthest away from the plane.

            MeshCollider mc = _part.collider as MeshCollider;
            BoxCollider bc = _part.collider as BoxCollider;

            if (mc)
            {
                print("This part has a mesh collider");
                foreach (Vector3 v in mc.sharedMesh.vertices)
                {
                    Vector3 vInWorld = mc.transform.localToWorldMatrix.MultiplyPoint3x4(v);
                    print("Considering vertex: " + vInWorld.ToString());
                    float d = GetVertixDistanceFromPlane(vInWorld, normal, pointInPlane);
                    if (d > maxd)
                    {
                        maxd = d;
                    }
                }
            }
            else if (bc)
            {
                // TODO support box colliders (whatever they are!)
                print("TODO: box colliders not yet supported");
            }
            else
            {
                // TODO
                // Debug.Log("generic collider "+c);
                // addPayload(c.bounds, Matrix4x4.identity);
                print("TODO: generic colliders not yet supported");
            }

            // Also consider all other attached parts
            {
                float d = ProcessParts(_part, refferingPart, normal);
                if(d>maxd) { maxd = d;}
            }

            print("Leaving ProcessPart. part: " + _part.name + " maxd: " + maxd);

            return (maxd);
        }

        private float GetVertixDistanceFromPlane(Vector3 point, Vector3 normal, Vector3 pointInPlane)
        {
            float d = Vector3.Dot((pointInPlane - point), normal) / Vector3.Dot(normal, normal);

            Vector3 intersect = (d * normal) + point;

            return (Vector3.Magnitude(point - intersect));
        }


        public void OnGUI()
        {
           /* EditorLogic editor = EditorLogic.fetch;
            if (editor == null) return;
            if (editor.editorScreen != EditorLogic.EditorScreen.Parts) return;

            osd.Update();
            */
        }











    }
}
