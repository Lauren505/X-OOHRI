using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKInner : MonoBehaviour
{
    public StretchKinematicChain stretchKinematicChain;

    [ReadOnly] public float lift;
    [ReadOnly] public float extension;
    [ReadOnly] public float yaw;
    [ReadOnly] public float pitch;
    [ReadOnly] public float roll;

    float GetEndEffectorLift()
    {
        return transform.localPosition.y;
    }


    public float GetClampedEndEffectorLift()
    {
        return Mathf.Clamp(lift, stretchKinematicChain.JointLift.min, stretchKinematicChain.JointLift.max);
    }


    float GetEndEffectorExtension()
    {
        return transform.localPosition.z / 4f;
    }

    public float GetClampedEndEffectorExtension()
    {
        return Mathf.Clamp(extension, stretchKinematicChain.WristExtensionL0.min, stretchKinematicChain.WristExtensionL0.max);
    }


    float GetEndEffectorPitchInRad()
    {
        var rotatedZAxis = transform.localRotation * Vector3.forward;
        var projectedZAxisInYZPlane = new Vector3(0f, rotatedZAxis.y, rotatedZAxis.z).normalized;
        float dotProduct = Vector3.Dot(Vector3.forward, projectedZAxisInYZPlane);
        float angleInRadians = Mathf.Acos(dotProduct);
        Vector3 positiveAngleDirection = Vector3.Cross(Vector3.forward, Vector3.right); 
        float sign = Mathf.Sign(Vector3.Dot(positiveAngleDirection, projectedZAxisInYZPlane));

        return angleInRadians * sign;
    }

    public float GetClampedEndEffectorPitchInRad()
    {
        return Mathf.Clamp(pitch, stretchKinematicChain.JointWristPitch.min, stretchKinematicChain.JointWristPitch.max);
    }

    float GetEndEffectorYawInRad()
    {
        var rotatedZAxis = transform.localRotation * Vector3.forward;
        var projectedZAxisInXZ = new Vector3(rotatedZAxis.x, 0f, rotatedZAxis.z).normalized;
        float dotProduct = Vector3.Dot(Vector3.forward, projectedZAxisInXZ);
        float angleInRadians = Mathf.Acos(dotProduct);
        Vector3 positiveAngleDirection = Vector3.Cross(Vector3.down, Vector3.forward);
        float sign = Mathf.Sign(Vector3.Dot(positiveAngleDirection, projectedZAxisInXZ));

        return angleInRadians * sign;
    }

    public float GetClampedEndEffectorYawInRad()
    {
        var clampedTo = Mathf.Clamp(yaw, stretchKinematicChain.JointWristYaw.min, stretchKinematicChain.JointWristYaw.max);
        return clampedTo;
    }


    float GetEndEffectorRollInRad()
    {
        var rotatedYAxis = transform.localRotation * Vector3.up;
        var projectedZAxisInXY = new Vector3(rotatedYAxis.x, rotatedYAxis.y, 0f).normalized;
        float dotProduct = Vector3.Dot(Vector3.up, projectedZAxisInXY);
        float angleInRadians = Mathf.Acos(dotProduct);
        Vector3 positiveAngleDirection = Vector3.Cross(Vector3.up, Vector3.forward);
        float sign = Mathf.Sign(Vector3.Dot(positiveAngleDirection, projectedZAxisInXY));

        return angleInRadians * sign;
    }

    public float GetClampedEndEffectorRollInRad()
    {
        return Mathf.Clamp(roll, stretchKinematicChain.JointWristRoll.min, stretchKinematicChain.JointWristRoll.max);
    }
    
    void Update()
    {
        // Debug.Log($"lift: {GetEndEffectorLift()}, extension: {GetEndEffectorExtension()} pitch: {GetEndEffectorPitchInRad()}, yaw: {GetEndEffectorYawInRad()}, roll: {GetEndEffectorRollInRad()}");

        lift = GetEndEffectorLift();
        extension = GetEndEffectorExtension();
        yaw = GetEndEffectorYawInRad();
        pitch = GetEndEffectorPitchInRad();
        roll = GetEndEffectorRollInRad();

        var lift_clamped = GetClampedEndEffectorLift();
        var extension_clamped = GetClampedEndEffectorExtension();
        var yaw_clamped = GetClampedEndEffectorYawInRad();
        var pitch_clamped = GetClampedEndEffectorPitchInRad();
        var roll_clamped = GetClampedEndEffectorRollInRad();

        stretchKinematicChain.SetAllJointStatesFromRosValues(lift_clamped, extension_clamped, yaw_clamped, pitch_clamped, roll_clamped, 0f);
    }
}
