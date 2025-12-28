using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XFoam : XObject
{
    // Additional attributes
    public Vector3 ApproxDimensions;   // meters
    public bool IsGraspable;
    public bool IsStackable;
    public bool IsRigid;
    public bool canResolve;
    public string ShapeType;           // Geometric primitive
    public float FrictionCoefficient;  // Sliding on desk

    void Awake()
    {
        Name = "Foam block";
        Weight = 120f;                           // Lightweight foam or plastic block
        Material = "Plastic or Foam";
        HasHandle = false;
        IsReceptacle = false;

        ShapeType = "Triangular Prism";
        IsGraspable = true;
        IsStackable = true;
        IsRigid = true;                          // Assuming hard plastic; still ok if foam
        canResolve = true;

        // Rough size estimate from image
        ApproxDimensions = new Vector3(0.08f, 0.06f, 0.10f);
        FrictionCoefficient = 0.5f;              // Plastic/foam against desk

        actions = new List<string>()
        {
            "fetch",
            "stack",
            "throw",
            "rotate",
            "slide",
            "push",
            "inspect"
        };

        Init();
    }

    // --- Affordance Methods ---

    public bool CanBePickedByGripper(float gripperWidthM)
    {
        // Width corresponds to the longest flat face
        return gripperWidthM >= Mathf.Max(ApproxDimensions.x, ApproxDimensions.z);
    }

    public bool CanBeStackedOn(float surfaceInclineDeg)
    {
        // Triangular prisms stack poorly unless flat on rectangular face
        return IsStackable && surfaceInclineDeg < 5f;
    }

    public bool CanBeSlid(float appliedForceN)
    {
        float massKg = Weight / 1000f;
        float requiredForce = massKg * 9.81f * FrictionCoefficient;
        return appliedForceN >= requiredForce;
    }

    public bool CanBeStoodUpright()
    {
        // Upright on a triangular tip is unlikely; stable on rectangular face
        return true;
    }

    // Rotate into a desired orientation for stacking or placement
    public bool CanRotateToOrientation(Vector3 desiredEulerAngles)
    {
        return true; // No orientation restrictions for block objects
    }
    public override string GetClassName()
    {
        return "Foam block";
    }
}