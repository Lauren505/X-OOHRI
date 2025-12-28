using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XBook : XObject
{
    // Additional attributes
    public Vector3 ApproxDimensions;     // meters (W, H, D)
    public int PageCount;                // Estimated
    public bool IsFlexible;              // Can it bend?
    public bool IsOpenable;              // Can be opened/read
    public string CoverType;             // Paperback / hardcover
    public bool IsStackable;             // Can it be stacked with others?
    public bool IsReadable;              // For context / semantic robots
    public float FrictionCoefficient;    // For sliding on surfaces

    void Awake()
    {
        Name = "Book";
        Weight = 450f;                        // Typical paperback ~400–500 g
        Material = "Paper";
        HasHandle = false;
        IsReceptacle = false;

        PageCount = 350;                      // Approximation
        IsFlexible = true;                    // Paper bends
        IsOpenable = true;
        IsStackable = true;
        IsReadable = true;

        CoverType = "Paperback";

        ApproxDimensions = new Vector3(0.16f, 0.025f, 0.24f);
        FrictionCoefficient = 0.4f;           // Paper on desk surface

        actions = new List<string>()
        {
            "fetch",
            "stack",
            "open",
            "inspect",
            "close",
            "flip_pages",
            "slide"
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

    public bool CanBeStackedOn(float topSurfaceInclineDeg)
    {
        return IsStackable && topSurfaceInclineDeg < 10f;
    }

    public bool CanBeSlid(float appliedForceN)
    {
        // Compute required force to overcome friction:
        float massKg = Weight / 1000f;
        float requiredForce = massKg * 9.81f * FrictionCoefficient;
        return appliedForceN >= requiredForce;
    }

    public bool CanSupportObjectOnTop(float objectWeightGrams)
    {
        // Books can support modest loads without bending
        return objectWeightGrams < 2000f; // about 2 kg
    }
    public override string GetClassName()
    {
        return "Book";
    }
}