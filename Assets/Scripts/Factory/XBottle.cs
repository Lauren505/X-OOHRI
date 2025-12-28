using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XBottle : XObject
{
    // Additional attributes
    public float VolumeML;               // Capacity
    public float FillLevelRatio;         // 0.0–1.0 estimate
    public bool IsOpenable;              // Screw cap
    public bool IsLiquidContainer;       // Holds liquid
    public string Shape;                 // Overall geometry
    public Vector3 ApproxDimensions;     // In meters
    public bool IsCompressible;          // Plastic flexes under force

    void Awake()
    {
        Name = "Plastic Bottle";
        Weight = 300f;                       // ~250ml water + ~50g bottle (rough)
        Material = "PET Plastic";
        HasHandle = false;
        IsReceptacle = true;

        VolumeML = 500f;
        FillLevelRatio = 0.25f;              // About 1/4 full based on image
        IsOpenable = true;
        IsLiquidContainer = true;
        IsCompressible = true;

        Shape = "Cylindrical Bottle with Screw Cap";
        ApproxDimensions = new Vector3(0.07f, 0.25f, 0.07f);

        actions = new List<string>()
        {
            "open",
            "close",
            "fetch",
            "tilt",
            "pour",
            "carry",
            "rotate_cap",
            "shake",
            "squeeze"
        };

        Init();
    }

    // --- Affordance Methods ---

    public bool CanPick()
    {
        return Weight < 1500f;
    }

    public string GetPreferredGraspType()
    {
        return "Cylindrical wrap or parallel gripper grasp";
    }

    public bool CanOpenCap()
    {
        return IsOpenable;
    }

    public bool CanPour()
    {
        return IsLiquidContainer && FillLevelRatio > 0f;
    }

    public bool IsStableOnSurface(float inclineDeg)
    {
        return inclineDeg < 5f; // Tall bottles tip over easily
    }

    public bool CanBeSqueezed()
    {
        return IsCompressible;
    }
    public override string GetClassName()
    {
        return "Bottle";
    }
}