using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XCarton : XObject
{
    public float VolumeML;          // Estimated fill volume in milliliters
    public bool IsOpenable;         // Twist-cap lid
    public bool IsLiquidContainer;  // Can hold liquid
    public string Shape;            // General geometry
    public Vector3 ApproxDimensions; // In meters

    void Awake()
    {
        Name = "Water Carton";
        Weight = 550f;             // ~500ml water + lightweight carton (~50g)
        Material = "Paperboard and Plastic Cap";
        HasHandle = false;
        IsReceptacle = true;       // Holds liquid

        VolumeML = 500f;
        IsOpenable = true;
        IsLiquidContainer = true;
        Shape = "Rectangular Prism with Screw Cap";
        ApproxDimensions = new Vector3(0.065f, 0.20f, 0.065f); // Rough estimate

        actions = new List<string>()
        {
            "open",
            "fetch",
            "tilt",
            "pour",
            "rotate_cap",
            "close"
        };

        Init();
    }

    // --- Affordance Methods ---

    // Can the robot safely pick up the object?
    public bool CanPick()
    {
        return Weight < 2000f; // Under 2kg is typically safe for small grippers
    }

    // The carton can be grasped from the sides
    public string GetPreferredGraspType()
    {
        return "Side pinch or parallel gripper grasp";
    }

    // The cap can be twisted to open
    public bool CanOpenCap()
    {
        return IsOpenable;
    }

    // Pouring action (requires orientation control)
    public bool CanPour()
    {
        return IsLiquidContainer;
    }

    // Surface safety check for placement
    public bool CanBePlacedOnSurface(float surfaceInclineDeg)
    {
        return surfaceInclineDeg < 10f; // Cartons fall over easily on uneven surfaces
    }

    public override string GetClassName()
    {
        return "Box";
    }
}