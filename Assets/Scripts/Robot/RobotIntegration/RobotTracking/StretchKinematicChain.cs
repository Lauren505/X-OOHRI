using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StretchKinematicChain : MonoBehaviour
{
    public Joint1DoF JointLift;
    public Joint1DoF WristExtensionL3;
    public Joint1DoF WristExtensionL2;
    public Joint1DoF WristExtensionL1;
    public Joint1DoF WristExtensionL0;
    public Joint1DoF JointWristYaw;
    public Joint1DoF JointWristPitch;
    public Joint1DoF JointWristRoll;
    public Joint1DoF JointGripperFingerLeft;
    public Joint1DoF JointGripperFingerRight;
    public Transform NeutralToBaseTransform;

    public TextMeshPro extensionLabel;

    [SerializeField] private float _jointLift;
    [SerializeField] private float _wristExtension;
    [SerializeField] private float _jointWristYaw;
    [SerializeField] private float _jointWristPitch;
    [SerializeField] private float _jointWristRoll;
    [SerializeField] private float _jointGripperFingerLeft;

    public void SetAllJointStatesFromRosValues(float jointLift, float wristExtension, float jointWristYaw, float jointWristPitch, float jointWristRoll, float jointGripperFingerLeft)
    {

        _jointLift = jointLift;
        _wristExtension = wristExtension;
        _jointWristYaw = jointWristYaw;
        _jointWristPitch = jointWristPitch;
        _jointWristRoll = jointWristRoll;
        _jointGripperFingerLeft = jointGripperFingerLeft;
    }

    public float GetLift()
    {
        return _jointLift;
    }

    public float GetExtension()
    {
        return _wristExtension;
    }

    public float GetYaw()
    {
        return _jointWristYaw;
    }

    public float GetPitch()
    {
        return _jointWristPitch;
    }

    public float GetRoll()
    {
        return _jointWristRoll;
    }

    public float GetGripper()
    {
        return _jointGripperFingerLeft;
    }


    public void Update()
    {
        if (JointLift != null)
            JointLift.SetTransformValue(JointLift.GetUnityTransformFromRosValue(_jointLift));

        if (WristExtensionL3 != null)
            WristExtensionL3.SetTransformValue(WristExtensionL3.GetUnityTransformFromRosValue(_wristExtension));
        if (WristExtensionL2 != null)
            WristExtensionL2.SetTransformValue(WristExtensionL2.GetUnityTransformFromRosValue(_wristExtension));
        if (WristExtensionL1 != null)
            WristExtensionL1.SetTransformValue(WristExtensionL1.GetUnityTransformFromRosValue(_wristExtension));
        if (WristExtensionL0 != null)
            WristExtensionL0.SetTransformValue(WristExtensionL0.GetUnityTransformFromRosValue(_wristExtension));

        if (JointWristYaw != null)
            JointWristYaw.SetTransformValue(JointWristYaw.GetUnityTransformFromRosValue(_jointWristYaw));
        if (JointWristPitch != null)
            JointWristPitch.SetTransformValue(JointWristPitch.GetUnityTransformFromRosValue(_jointWristPitch));
        if (JointWristRoll != null)
            JointWristRoll.SetTransformValue(JointWristRoll.GetUnityTransformFromRosValue(_jointWristRoll));

        if (JointGripperFingerLeft != null)
            JointGripperFingerLeft.SetTransformValue(JointGripperFingerLeft.GetUnityTransformFromRosValue(_jointGripperFingerLeft));
        if (JointGripperFingerRight != null)
            JointGripperFingerRight.SetTransformValue(JointGripperFingerRight.GetUnityTransformFromRosValue(_jointGripperFingerLeft));

        if (extensionLabel != null)
            extensionLabel.SetText($"ext: {_wristExtension.ToString("F2")}");


    }
}
