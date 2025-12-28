using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Mathematics;
using UnityEngine;

public class XStretchRobot : XRobot
{
    IWalkableArea _walkable;

    void Awake()
    {
        // Whole robot
        Size = new Vector3(33f, 34f, 141f); // cm (W x L x H)
        Weight = 24500f; // grams (24.5 kg)
        MinimumRuntime = 7200f; // seconds (2 hours = heavy CPU load)
        GroundClearance = 0.025f; // m

        // Joints & Gripper
        GripperPayload = 2000f; // grams
        GripperMaxAperture = 0.15f; // m
        VerticalRange = (0f, 1.10f); // m (Lift range)
        HorizontalRange = (0f, 0.51f); // m (Arm extension range)
        WristYawRange = (0f, Mathf.Deg2Rad * 330f); // radians
        WristPitchRange = (0f, Mathf.Deg2Rad * 150f); // radians
        WristRollRange = (0f, Mathf.Deg2Rad * 345f); // radians
    }
    public void Init(IWalkableArea walkable) => _walkable = walkable;
    public List<int> Place(XObject xobject, Vector3 baseCenter)
    {
        List<int> errorCodes = new List<int>();
        if (_walkable.PointOccluded(xobject)) errorCodes.Add(5);
        if (_walkable.PointInHeightLimit(xobject.Position)) errorCodes.Add(3);
        if (!_walkable.PointInNavMesh(baseCenter)) errorCodes.Add(4);

        return errorCodes;
    }

    public List<int> Pick(XObject xobject, Vector3 baseCenter)
    {
        List<int> errorCodes = new List<int>();
        // Is object smaller than gripper aperture?
        if (Mathf.Min(Mathf.Min(xobject.Size.x, xobject.Size.y), xobject.Size.z) > GripperMaxAperture && !xobject.HasHandle) errorCodes.Add(1);
        // Is object weight below gripper payload?
        if (xobject.Weight > GripperPayload) errorCodes.Add(2);
        // Is object reachable in NavMesh?
        if (_walkable.PointInHeightLimit(xobject.Position)) errorCodes.Add(3);
        // Is object reachable in height?
        if (!_walkable.PointInNavMesh(baseCenter)) errorCodes.Add(4);

        return errorCodes;
    }
    public List<int> Grab(XObject xobject, Vector3 baseCenter)
    {
        List<int> errorCodes = new List<int>();
        //if (_walkable.PointInHeightLimit(xobject.Position)) errorCodes.Add(3);
        if (!_walkable.PointInNavMesh(baseCenter)) errorCodes.Add(4);

        return errorCodes;
    }
}
