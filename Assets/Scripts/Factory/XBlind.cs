using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XBlind : XObject
{
    // Additional attributes
    public Vector2 WindowDimensions;       // Width & height of the blinds (meters)
    public bool IsAdjustable;              // Can slats be tilted?
    public bool IsRaiseLowerable;          // Can the blinds be lifted or lowered?
    public bool HasPullCord;               // Mechanism for control
    public bool IsFragile;                 // Slats can bend or break
    public string BlindMaterial;           // Likely plastic or aluminum
    public float SlatWidth;                // Size of each horizontal slat
    public bool IsWallMounted;

    void Awake()
    {
        Name = "Window Blinds";
        Weight = 3000f;                          // Entire blind assembly ~3 kg
        Material = "Plastic or Aluminum Slats";
        BlindMaterial = "Plastic or Aluminum";

        HasHandle = false;                       // No conventional handle
        HasPullCord = true;
        IsReceptacle = false;
        IsWallMounted = true;

        IsAdjustable = true;                     // Slats tilt
        IsRaiseLowerable = true;
        IsFragile = true;                        // Thin slats bend easily

        // Rough dimensions based on the window
        WindowDimensions = new Vector2(1.2f, 1.5f);  // width x height
        SlatWidth = 0.03f;

        actions = new List<string>()
        {
            "dim",
            "brighten",
            "draw",
            "raise",
            "lower",
            "inspect"
        };

        Init();
    }

    // --- Affordance Methods ---

    public bool CanTiltSlats()
    {
        return IsAdjustable;
    }

    public bool RequiresGentleHandling()
    {
        return IsFragile;
    }

    public bool CanBeRaisedLowered()
    {
        return IsRaiseLowerable;
    }

    public bool CanBeInteractedWithByRobotArm(float reachHeightMeters)
    {
        // Must be able to reach the pull cord area
        return reachHeightMeters >= 1.4f;
    }

    public bool CanBlockLight()
    {
        return true; // Primary function
    }

    public bool CanProvidePartialPrivacy()
    {
        return true;
    }
    public override string GetClassName()
    {
        return "Blind";
    }
}