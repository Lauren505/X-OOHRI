using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XBasket : XObject
{
    // Extra attributes
    public Vector3 ApproxDimensions;       // meters (W, H, D)
    public bool IsFlexible;                // Soft sides can deform
    public bool IsContainer;               // Holds items
    public bool HasDualHandles;            // Two soft handles
    public float VolumeLiters;             // Approx storage volume
    public string BasketMaterial;          // Woven rope/cotton
    public bool IsLightweight;

    void Awake()
    {
        Name = "Woven Basket";
        Weight = 600f;                          // Soft rope basket ~0.5–0.8 kg
        Material = "Woven Cotton Rope";
        BasketMaterial = "Cotton Rope";

        HasHandle = true;                        // Built-in soft handles
        HasDualHandles = true;
        IsReceptacle = true;                     // It holds items
        IsContainer = true;

        IsFlexible = true;                       // Sides collapse if squeezed
        IsLightweight = true;

        // Approx size from image
        ApproxDimensions = new Vector3(0.40f, 0.30f, 0.35f);
        VolumeLiters = 30f;                      // Rough estimate for storage

        actions = new List<string>()
        {
            "pull",
            "push",
            "fetch",
            "carry",
            "insert_item",
            "remove_item",
            "drag",
            "compress",
            "stow"
        };

        Init();
    }

    // --- Affordance Methods ---

    public bool CanBePickedBySideGrasp(float gripperWidthM)
    {
        // Robot can pinch the rim if gripper width is enough
        return gripperWidthM >= 0.02f;
    }

    public bool CanBeLiftedByHandle(float robotGripForceN)
    {
        // Handles are fabric; too much force can tear
        return robotGripForceN <= 40f;
    }

    public bool CanHoldItem(float itemWeightGrams)
    {
        // These baskets safely hold several kg
        return itemWeightGrams <= 10000f; // ~10 kg load
    }

    public bool CanBeDragged(float robotForceN)
    {
        // Soft basket drags easily on carpet
        float dragThreshold = 5f; // Small force needed
        return robotForceN >= dragThreshold;
    }

    public bool CanAcceptItemWithDimensions(Vector3 itemDimsM)
    {
        return itemDimsM.x < ApproxDimensions.x &&
               itemDimsM.z < ApproxDimensions.z &&
               itemDimsM.y < ApproxDimensions.y;
    }
    public override string GetClassName()
    {
        return "Basket";
    }
}