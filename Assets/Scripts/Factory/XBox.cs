using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XBox : XObject
{
    // Additional attributes
    public Vector3 ApproxDimensions;      // meters (W, H, D)
    public bool IsOpenable;              // Lid can be lifted
    public bool ContainsElectronics;     // Sensitive contents
    public bool IsFragile;               // Box contents may be fragile
    public bool IsCardboard;             // Box material
    public string ShapeType;             // Rectangular prism
    public bool HasPullTab;              // Cardboard pull handle
    public float FrictionCoefficient;    // Sliding on desk
    public bool IsSealed;                // If box is closed

    void Awake()
    {
        Name = "Box";
        Weight = 1800f;                       // ~1.5–2.0 kg for headset + packaging
        Material = "Cardboard";
        IsCardboard = true;

        HasHandle = true;                      // Cardboard pull tab counts
        HasPullTab = true;
        IsReceptacle = true;                   // Contains items
        IsOpenable = true;
        IsFragile = true;                      // Contents may be delicate
        ContainsElectronics = true;

        ShapeType = "Rectangular Prism";

        ApproxDimensions = new Vector3(0.28f, 0.18f, 0.18f);
        FrictionCoefficient = 0.45f;
        IsSealed = true;

        actions = new List<string>()
        {
            "stack",
            "fetch",
            "open",
            "close",
            "slide",
            "lift",
            "tilt",
            "inspect",
            "carry"
        };

        Init();
    }

    // --- Affordance Methods ---

    public bool CanBePickedByGripper(float gripperWidthM)
    {
        return gripperWidthM >= ApproxDimensions.x;
    }

    public bool CanBeOpened()
    {
        return IsOpenable;
    }

    public bool CanBeSlid(float appliedForceN)
    {
        float massKg = Weight / 1000f;
        float requiredForce = massKg * 9.81f * FrictionCoefficient;
        return appliedForceN >= requiredForce;
    }

    public bool RequiresGentleHandling()
    {
        return ContainsElectronics;
    }

    public bool CanSupportItemOnTop(float itemWeightGrams)
    {
        // Cardboard box can support moderate weight
        return itemWeightGrams <= 5000f; // 5 kg
    }

    public bool CanBeCarriedUsingTab(float gripForceN)
    {
        // Pull tab should not be overstressed
        return gripForceN <= 40f;
    }
    public override string GetClassName()
    {
        return "Box";
    }
}