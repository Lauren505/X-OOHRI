using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRobot : MonoBehaviour
{
    // Whole robot
    public Vector3 Size { get; protected set; } // m
    public float Weight { get; protected set; } // grams
    public float MinimumRuntime { get; protected set; } // seconds
    public float GroundClearance { get; protected set; } // m

    // Joints & Gripper
    public float GripperPayload { get; protected set; } // grams
    public float GripperMaxAperture { get; protected set; } // grams
    public (float min, float max) VerticalRange { get; protected set; } // m
    public (float min, float max) HorizontalRange { get; protected set; } // m
    public (float min, float max) WristYawRange { get; protected set; } // rad
    public (float min, float max) WristPitchRange { get; protected set; } // rad
    public (float min, float max) WristRollRange { get; protected set; } // rad
}
