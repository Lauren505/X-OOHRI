using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRPlugin;

public class AlignRobotUnity : MonoBehaviour
{
    public Transform robotCalib;
    public RobotROS robotRos;

    [Header("Alignment")]
    public Pose robotRosPose;
    private Vector3 alignedPosition = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 pos_initialRos = new Vector3(0.0f, 0.0f, 0.0f);
    private Quaternion rot_initialRos = Quaternion.identity;
    public Vector3 newPos;
    public Quaternion newRot;
    
    [Header("Relative Pose")]
    private Vector3 pos_ROSRelToUnity;
    private Quaternion rot_ROSRelToUnity;
    private Vector3 pos_UnityRelToRos;
    private Quaternion rot_UnityRelToRos;

    [Header("Debug")]
    public DebugRobotCoords debugRobotCoords;

    void Update()
    {
        RosRelToUnity();
        UnityRelToRos();
        robotRosPose = robotRos.GetRobotRosPose();

        alignedPosition = robotCalib.localPosition - rot_initialRos * pos_initialRos;
        newPos = GetRosToUnityPos(robotRosPose.position);
        newRot = GetRosToUnityRot(robotRosPose.rotation);
        transform.SetPositionAndRotation(newPos, newRot);
        debugRobotCoords.AppendAMCLDebugPose(robotRosPose.position, robotRosPose.rotation);
    }
    public Pose GetUnityToRos(Pose pose)
    {
        Vector3 rosPos = GetUnityToRosPos(pose.position);
        Quaternion rosRot = GetUnityToRosRot(pose.rotation);
        return new Pose(rosPos, rosRot);
    }
    public Vector3 GetUnityToRosPos(Vector3 position)
    {
        Vector3 rosPos = Quaternion.Inverse(rot_initialRos) * (position - alignedPosition);
        return rosPos;
    }
    public Quaternion GetUnityToRosRot(Quaternion rotation)
    {
        Quaternion rosRot = Quaternion.Inverse(rot_initialRos) * rotation;
        return rosRot;
    }

    public Vector3 GetRosToUnityPos(Vector3 position)
    {
        Vector3 unityPos = rot_initialRos * position + alignedPosition;
        return unityPos;
    }
    public Quaternion GetRosToUnityRot(Quaternion rotation)
    {
        Quaternion unityRot = rot_initialRos * rotation;
        return unityRot;
    }

    private void RosRelToUnity()
    {
        pos_ROSRelToUnity = robotCalib.InverseTransformPoint(robotRosPose.position);
        rot_ROSRelToUnity = Quaternion.Inverse(robotCalib.localRotation) * robotRosPose.rotation;
    }
    private void UnityRelToRos()
    {
        pos_UnityRelToRos = robotCalib.position - (rot_UnityRelToRos * robotRosPose.position);
        rot_UnityRelToRos = robotCalib.localRotation * Quaternion.Inverse(robotRosPose.rotation);
    }

    public void SaveInitialOffset()
    {
        pos_initialRos = robotRosPose.position;
        rot_initialRos = rot_UnityRelToRos;
    }
}
