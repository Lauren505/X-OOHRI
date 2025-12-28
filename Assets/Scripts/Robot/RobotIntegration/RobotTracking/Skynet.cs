using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using System.Linq;
using TMPro;

public enum RobotState
{
    Idle,
    StowingToCurrentHeight,
    StowingToLow,
    CoarseNAVing,
    CoarseIKingToHeight,
    CoarseIKingToGripperRotationAndGripYaw,
    CoarseIKingToGripperRotationAndGripPitchRoll,
    CorrectiveRotating,
    CoarseIKingToExtension,
    Grasping
}

public enum RobotIntentPhase
{
    Navigation,
    Acquisition,
    Drop,
    Idle
}

public enum Operation
{
    Acquisition,
    Drop
}

// skynet event handlers
public delegate void ObjectAcquired(string jobName);
public delegate void ObjectDropped(string jobName);

public delegate void RotationFinished();


public class Skynet : MonoBehaviour
{

    public GameObject robotPreview;
    public GameObject robotTarget;
    public GameObject robotCurrent;

    public RobotROS robotROS; 

    private StretchKinematicChain stretchKinematicChain;

    internal IKInner innerTargetIK;
    private GlobalEndEffectorKinematics globalEndEffectorKinematics_Drop;
    private GlobalEndEffectorKinematics globalEndEffectorKinematics_Acquisition;
    private AlignRobotUnity alignRobotUnity;

    private Pose? _navigationTarget;
    private Vector3? _navigationCandidatePosition;
    private Quaternion? _navigationCandidateRotation;

    public RobotState robotState = RobotState.Idle;
    public RobotIntentPhase robotIntentPhase = RobotIntentPhase.Idle;

    public Vector3 AcquisitionBasePosition;
    public Quaternion AcquisitionBaseRotation;
    public Vector3 AcquisitionEndEffectorPosition;
    public Quaternion AcquisitionEndEffectorRotation;

    public Vector3 DropBasePosition;
    public Quaternion DropBaseRotation;
    public Vector3 DropEndEffectorPosition;
    public Quaternion DropEndEffectorRotation;

    [Header("Debug")]
    public bool editorRequestedToggleTargetDisplay;

    public ObjectAcquired objectAcquired;
    public ObjectDropped objectDropped;

    public Vector3 preAcquisitionPosition;
    public Quaternion preAcquisitionRotation;

    public float gripperAperture = 0.7f;

    private string currentJobName;

    private bool overrideJoints = false;

    void Start()
    {
        var lineParent = new GameObject("PathRendererContainer-potential");
        lineParent.transform.parent = gameObject.transform;
        lineParent.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);

        robotROS = robotCurrent.GetComponent<RobotROS>();
        robotROS.rosNavigationGoalFinished += RosNavigationGoalFinished;
        robotROS.rosJointTrajectoryGoalFinished += RosJointTrajectoryGoalFinished;

        innerTargetIK = robotTarget.GetComponentInChildren<IKInner>();

        globalEndEffectorKinematics_Acquisition = GameObject.Find("GlobalEndEffectorKinematics-Acquisition").GetComponent<GlobalEndEffectorKinematics>();
        globalEndEffectorKinematics_Drop = GameObject.Find("GlobalEndEffectorKinematics-Drop").GetComponent<GlobalEndEffectorKinematics>();
        globalEndEffectorKinematics_Acquisition.iKPoseUpdated += AcquisitionIKUpdated;
        globalEndEffectorKinematics_Drop.iKPoseUpdated += DropIKUpdated;
        globalEndEffectorKinematics_Acquisition.grabPoseUpdated += GrabIKUpdated;

        stretchKinematicChain = robotCurrent.GetComponent<StretchKinematicChain>();
        alignRobotUnity = robotCurrent.GetComponent<AlignRobotUnity>();

        robotState = RobotState.Idle;
        robotIntentPhase = RobotIntentPhase.Idle;
    }

    void UpdateRosNavigation(Vector3 targetPosition, float yaw)
    {
        robotROS.NavigateToPose(targetPosition, yaw);
    }

    public bool ikUpdateRequestedViaEditor;
    public bool invokeEnterRotation;

    private void Update()
    {
        if (ikUpdateRequestedViaEditor)
        {
            // IKOuterUpdated(new Vector3(0.0f, 0.0f, 0.2f), Quaternion.identity, new Vector3(1f, 0.5f, -0.0187f), Quaternion.Euler(new Vector3(1.52f, 90.04f, 0.11f)));
            AcquisitionIKUpdated(
                AcquisitionBasePosition,
                AcquisitionBaseRotation,
                AcquisitionEndEffectorPosition,
                AcquisitionEndEffectorRotation,
                "editor"
            );
            ikUpdateRequestedViaEditor = false;
        }

        if (invokeEnterRotation)
        {
            invokeEnterRotation = false;
            EnterRotation();
        }
    }

    private void SetNewState(RobotState newState) {
        var prevState = robotState;
        robotState = newState;
        var logText = $"Transitioned from {prevState}/{robotIntentPhase} to {newState}/{robotIntentPhase}";
        Debug.Log(logText);
    }

    private void SetNewStateAndIntentPhase(RobotState newState, RobotIntentPhase newPhase)
    {
        var prevState = robotState;
        var prevPhase = robotIntentPhase;
        robotState = newState;
        robotIntentPhase = newPhase;
        var logText = $"Transitioned from {prevState}/{prevPhase} to {newState}/{newPhase}";
        Debug.Log(logText);
    }

    private void UndisplayRobotPreview() {
        _navigationCandidatePosition = null;
        _navigationCandidateRotation = null;
        robotPreview.transform.SetPose(new Pose(new Vector3(1000, 1000, 1000), robotPreview.transform.rotation));
    }

    #region "Interaction Events"
    internal void NavigationCandidatePositionSelected(Vector3 position)
    {
        //_navigationCandidatePosition = alignRobotUnity.GetUnityToRosPos(position);
         _navigationCandidatePosition = position;
    }

    internal void NavigationCandidateDiscarded()
    {
        _navigationCandidatePosition = null;
        _navigationCandidateRotation = null;
        robotPreview.transform.SetPose(new Pose(new Vector3(1000, 1000, 1000), robotPreview.transform.rotation));
    }

    internal void NavigationCandidateRotationSelected(Quaternion rotation)
    {
        _navigationCandidateRotation = Quaternion.Euler(0f, robotTarget.transform.rotation.eulerAngles.y, 0f) * rotation; 

        robotPreview.transform.SetPose(new Pose(
            _navigationCandidatePosition.Value,
            _navigationCandidateRotation.Value
        ));
    }
    #endregion

    #region "Robot State Machine"

    internal void NavigationTargetSelected()
    {
        Debug.Log($"NavigationTargetSelected @ {robotState}/{robotIntentPhase}");

        switch (robotIntentPhase)
        {
            case RobotIntentPhase.Idle:
            case RobotIntentPhase.Navigation:
            case RobotIntentPhase.Acquisition:
            case RobotIntentPhase.Drop:
                _navigationTarget = new Pose(_navigationCandidatePosition.Value, _navigationCandidateRotation.Value);
                robotTarget.transform.SetPose(_navigationTarget.Value);
                UndisplayRobotPreview();

                // Uncomment below to bypass joints for navigation
                //SetNewStateAndIntentPhase(RobotState.CoarseNAVing, RobotIntentPhase.Navigation);
                //if(robotTarget.transform.position.x == float.NaN || robotTarget.transform.position.y == float.NaN || robotTarget.transform.position.z == float.NaN){
                //    Debug.LogError($"nan at RosJointTrajectoryGoalFinished robotTarget.transform.position {robotTarget.transform.position.x} {robotTarget.transform.position.y} {robotTarget.transform.position.z}");
                //}
                //UpdateRosNavigation(robotTarget.transform.position, robotTarget.transform.rotation.eulerAngles.y);
                // Uncomment below for full skynet
                SetNewStateAndIntentPhase(RobotState.StowingToCurrentHeight, RobotIntentPhase.Navigation);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 0f, 3.0f, -0.62f, 0.0f, stretchKinematicChain.GetGripper());

                break;
        }
           

              
    }

    public void SetJobName(string currentJobName)
    {
        this.currentJobName = currentJobName; 
    }

    public void AcquisitionIKUpdated(
        Vector3 basePosition,
        Quaternion baseRotation,
        Vector3 endEffectorPosition,
        Quaternion endEffectorRotation,
        string jobName
    )
    {
        Debug.Log($"AcquisitionIKUpdated @ {robotState}/{robotIntentPhase}");

        AcquisitionBasePosition = basePosition;
        AcquisitionBaseRotation = baseRotation;
        AcquisitionEndEffectorPosition = endEffectorPosition + new Vector3(0.0f, 0.2f, 0.0f); // make sure robot doesn't collide with table
        AcquisitionEndEffectorRotation = endEffectorRotation;
        currentJobName = jobName;

        switch (robotIntentPhase)
        {
            case RobotIntentPhase.Idle:
            case RobotIntentPhase.Navigation:
            case RobotIntentPhase.Acquisition:
                robotTarget.transform.SetPositionAndRotation(AcquisitionBasePosition, AcquisitionBaseRotation);
                innerTargetIK.transform.SetPositionAndRotation(AcquisitionEndEffectorPosition, AcquisitionEndEffectorRotation);
                UndisplayRobotPreview();

                preAcquisitionPosition = robotCurrent.transform.position;
                preAcquisitionRotation = robotCurrent.transform.rotation;

                SetNewStateAndIntentPhase(RobotState.StowingToCurrentHeight, RobotIntentPhase.Acquisition);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 0f, 3.0f, -0.62f, 0.0f, 0f);
                break;

            case RobotIntentPhase.Drop:
                robotTarget.transform.SetPositionAndRotation(DropBasePosition, DropBaseRotation);
                innerTargetIK.transform.SetPositionAndRotation(DropEndEffectorPosition, DropEndEffectorRotation);
                UndisplayRobotPreview();

                SetNewState(RobotState.StowingToCurrentHeight);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 0f, 3.0f, -0.62f, 0.0f, stretchKinematicChain.GetGripper());
                break;
        }

    }

    public void DropIKUpdated(
     Vector3 basePosition,
     Quaternion baseRotation,
     Vector3 endEffectorPosition,
     Quaternion endEffectorRotation,
     string jobName
 )
    {
        Debug.Log($"DropIKUpdated @ {robotState}/{robotIntentPhase}");

        DropBasePosition = basePosition;
        DropBaseRotation = baseRotation;
        DropEndEffectorPosition = endEffectorPosition + new Vector3(0.0f, 0.2f, 0.0f); // make sure robot doesn't collide with table
        DropEndEffectorRotation = endEffectorRotation;
        currentJobName = jobName;

        switch (robotIntentPhase)
        {
            case RobotIntentPhase.Idle:
            case RobotIntentPhase.Navigation:
            case RobotIntentPhase.Acquisition:
                robotTarget.transform.SetPositionAndRotation(AcquisitionBasePosition, AcquisitionBaseRotation);
                innerTargetIK.transform.SetPositionAndRotation(AcquisitionEndEffectorPosition, AcquisitionEndEffectorRotation);
                UndisplayRobotPreview();

                SetNewStateAndIntentPhase(RobotState.StowingToCurrentHeight, RobotIntentPhase.Acquisition);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 0f, 3.0f, -0.62f, 0.0f, 0f);
                break;

            case RobotIntentPhase.Drop:
                robotTarget.transform.SetPositionAndRotation(DropBasePosition, DropBaseRotation);
                innerTargetIK.transform.SetPositionAndRotation(DropEndEffectorPosition, DropEndEffectorRotation);
                UndisplayRobotPreview();

                SetNewState(RobotState.StowingToCurrentHeight);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 0f, 3.0f, -0.62f, 0.0f, stretchKinematicChain.GetGripper());
                break;
        }

    }

    public void RosJointTrajectoryGoalFinished()
    {
        Debug.Log($"RosJointTrajectoryGoalFinished @ {robotState},{robotIntentPhase}");
        switch (robotState)
        {
            case RobotState.StowingToCurrentHeight:

                if (robotIntentPhase == RobotIntentPhase.Acquisition || robotIntentPhase == RobotIntentPhase.Drop) 
                {
                    SetNewState(RobotState.StowingToLow);
                    robotROS.FollowJointTrajectoryToCustom(0.5f, 4 * stretchKinematicChain.GetExtension(), 3.0f, stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), stretchKinematicChain.GetGripper()) ;
                } else if (robotIntentPhase == RobotIntentPhase.Idle || robotIntentPhase == RobotIntentPhase.Navigation)
                {
                    SetNewState(RobotState.StowingToLow);
                    robotROS.FollowJointTrajectoryToCustom(0.5f, 4 * stretchKinematicChain.GetExtension(), 3.0f, stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), 0f) ;
                }

                break;

            case RobotState.StowingToLow:

                if (robotIntentPhase == RobotIntentPhase.Acquisition || robotIntentPhase == RobotIntentPhase.Drop || robotIntentPhase == RobotIntentPhase.Navigation)
                {
                    SetNewState(RobotState.CoarseNAVing);
                    if(robotTarget.transform.position.x == float.NaN || robotTarget.transform.position.y == float.NaN || robotTarget.transform.position.z == float.NaN){
                        Debug.LogError($"nan at RosJointTrajectoryGoalFinished robotTarget.transform.position {robotTarget.transform.position.x} {robotTarget.transform.position.y} {robotTarget.transform.position.z}");
                    }
                    UpdateRosNavigation(alignRobotUnity.GetUnityToRosPos(robotTarget.transform.position), alignRobotUnity.GetUnityToRosRot(robotTarget.transform.rotation).eulerAngles.y);
                }
                else if (robotIntentPhase == RobotIntentPhase.Idle) 
                {
                    SetNewState(RobotState.CoarseNAVing);
                    UpdateRosNavigation(alignRobotUnity.GetUnityToRosPos(preAcquisitionPosition), alignRobotUnity.GetUnityToRosRot(preAcquisitionRotation).eulerAngles.y);
                }
                
                break;

            case RobotState.CoarseIKingToHeight:
                Debug.Log("Start Yaw");
                SetNewState(RobotState.CoarseIKingToGripperRotationAndGripYaw);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * stretchKinematicChain.GetExtension(), innerTargetIK.yaw, stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), stretchKinematicChain.GetGripper());
                break;

            case RobotState.CoarseIKingToGripperRotationAndGripYaw:
                SetNewState(RobotState.CoarseIKingToGripperRotationAndGripPitchRoll);
                if (robotIntentPhase == RobotIntentPhase.Acquisition)
                {
                    var pitchClamped = 0.1f;
                    robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * stretchKinematicChain.GetExtension(), stretchKinematicChain.GetYaw(), pitchClamped, innerTargetIK.roll, gripperAperture);
                }
                else
                {
                    var pitchClamped = Mathf.Clamp(innerTargetIK.pitch, stretchKinematicChain.JointWristPitch.min, stretchKinematicChain.JointWristPitch.max);
                    robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * stretchKinematicChain.GetExtension(), stretchKinematicChain.GetYaw(), pitchClamped, innerTargetIK.roll, stretchKinematicChain.GetGripper());
                }

                break;

            case RobotState.CoarseIKingToGripperRotationAndGripPitchRoll:
                // Uncomment to bypass corrective rotation
                //SetNewState(RobotState.CoarseIKingToExtension);
                //robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * innerTargetIK.extension, stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), stretchKinematicChain.GetGripper());
                // Uncomment for corrective rotation
                SetNewState(RobotState.CorrectiveRotating);
                var robotRotator = gameObject.AddComponent<RobotRotator>();
                robotRotator.rotationFinished = RotationFinished;
                break;

            case RobotState.CoarseIKingToExtension:
                SetNewState(RobotState.Grasping);
                if (robotIntentPhase == RobotIntentPhase.Acquisition)
                {
                    // gripping
                    robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * stretchKinematicChain.GetExtension(), stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), 0.1f);
                } else
                {
                    // droppping
                    robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * stretchKinematicChain.GetExtension(), stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), gripperAperture);
                }
                break;

            case RobotState.Grasping:

                if (robotIntentPhase == RobotIntentPhase.Acquisition)
                {
                    if (overrideJoints)
                    {
                        robotTarget.transform.SetPositionAndRotation(DropBasePosition, DropBaseRotation);
                        innerTargetIK.transform.SetPositionAndRotation(DropEndEffectorPosition, DropEndEffectorRotation);
                        SetNewStateAndIntentPhase(RobotState.CoarseNAVing, RobotIntentPhase.Drop);
                        UpdateRosNavigation(alignRobotUnity.GetUnityToRosPos(robotTarget.transform.position), alignRobotUnity.GetUnityToRosRot(robotTarget.transform.rotation).eulerAngles.y);
                    }
                    else
                    {// next up: deliver object
                        objectAcquired?.Invoke(currentJobName);
                        robotTarget.transform.SetPositionAndRotation(DropBasePosition, DropBaseRotation);
                        innerTargetIK.transform.SetPositionAndRotation(DropEndEffectorPosition, DropEndEffectorRotation);
                        SetNewStateAndIntentPhase(RobotState.StowingToCurrentHeight, RobotIntentPhase.Drop);

                        var lift_clamped = Mathf.Clamp(stretchKinematicChain.GetLift() + 0.05f, stretchKinematicChain.JointLift.min, stretchKinematicChain.JointLift.max);

                        robotROS.FollowJointTrajectoryToCustom(lift_clamped, 0f, stretchKinematicChain.GetYaw(), 0.0f, 0.0f, stretchKinematicChain.GetGripper());
                    }
                }
                else if (robotIntentPhase == RobotIntentPhase.Drop)
                {
                    objectDropped?.Invoke(currentJobName);
                    currentJobName = "";
                    SetNewStateAndIntentPhase(RobotState.StowingToCurrentHeight, RobotIntentPhase.Idle);
                    robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 0f, stretchKinematicChain.GetYaw(), 0f, 0.0f, stretchKinematicChain.GetGripper());
                }

                break;


            default:
                throw new InvalidOperationException($"Unexpected state transition in RosJointTrajectoryGoalFinished @ {robotState},{robotIntentPhase}!");
        }
    }

    public void RotationFinished()
    {
        switch (robotState)
        {
            case RobotState.CorrectiveRotating:
                Destroy(GetComponent<RobotRotator>());
                SetNewState(RobotState.CoarseIKingToExtension);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * innerTargetIK.extension, stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), stretchKinematicChain.GetGripper());
                break;
            default:
                if (GetComponent<RobotRotator>() != null)
                {
                    Destroy(GetComponent<RobotRotator>());
                }
                throw new InvalidOperationException($"Unexpected state transition in RosJointTrajectoryGoalFinished @ {robotState},{robotIntentPhase}!");
        }

    }
    
    public void RosNavigationGoalFinished()
    {
        Debug.Log($"RosNavigationGoalFinished @ {robotState},{robotIntentPhase}");

        switch (robotState)
        {
            case RobotState.CoarseNAVing:
                _navigationTarget = null;
                
                if (robotIntentPhase == RobotIntentPhase.Acquisition)
                {
                    if (overrideJoints)
                    {
                        SetNewState(RobotState.CoarseIKingToExtension);
                        robotROS.FollowJointTrajectoryToCustom(0.3f, 0.1f, stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), gripperAperture);
                    }
                    else
                    {
                        SetNewState(RobotState.CoarseIKingToHeight);
                        robotROS.FollowJointTrajectoryToCustom(innerTargetIK.lift, 4 * stretchKinematicChain.GetExtension(), stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), stretchKinematicChain.GetGripper());
                    }
                }
                else if (robotIntentPhase == RobotIntentPhase.Drop)
                {
                    if (overrideJoints)
                    {
                        SetNewStateAndIntentPhase(RobotState.Idle, RobotIntentPhase.Idle);
                        robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 4 * stretchKinematicChain.GetExtension(), stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), gripperAperture);
                        overrideJoints = false;
                    }
                    else
                    {
                        var lift_increase = -0.02f;
                        SetNewState(RobotState.CoarseIKingToHeight);
                        robotROS.FollowJointTrajectoryToCustom(innerTargetIK.lift + lift_increase, 4 * stretchKinematicChain.GetExtension(), stretchKinematicChain.GetYaw(), stretchKinematicChain.GetPitch(), stretchKinematicChain.GetRoll(), stretchKinematicChain.GetGripper());
                    }
                }
                else if (robotIntentPhase == RobotIntentPhase.Idle || robotIntentPhase == RobotIntentPhase.Navigation)
                {
                    SetNewState(RobotState.Idle);
                }

                break;
            default:
                throw new InvalidOperationException($"Unexpected state transition in RosNavigationGoalFinished @ {robotState},{robotIntentPhase}!");
        }
    }

    public void GrabIKUpdated(
        Vector3 acqBasePosition,
        Quaternion acqBaseRotation,
        Vector3 acqEndEffectorPosition,
        Quaternion acqEndEffectorRotation,
        Vector3 dropBasePosition,
        Quaternion dropBaseRotation,
        Vector3 dropEndEffectorPosition,
        Quaternion dropEndEffectorRotation,
        string jobName
    )
    {
        Debug.Log($"GrabIKUpdated @ {robotState}/{robotIntentPhase}");

        AcquisitionBasePosition = acqBasePosition;
        AcquisitionBaseRotation = acqBaseRotation;
        AcquisitionEndEffectorPosition = acqEndEffectorPosition + new Vector3(0.0f, 0.1f, 0.0f); // make sure robot doesn't collide with table
        AcquisitionEndEffectorRotation = acqEndEffectorRotation;
        DropBasePosition = dropBasePosition;
        DropBaseRotation = dropBaseRotation;
        DropEndEffectorPosition = dropEndEffectorPosition + new Vector3(0.0f, 0.1f, 0.0f); // make sure robot doesn't collide with table
        DropEndEffectorRotation = dropEndEffectorRotation;
        currentJobName = jobName;

        overrideJoints = true;

        switch (robotIntentPhase)
        {
            case RobotIntentPhase.Idle:
            case RobotIntentPhase.Navigation:
            case RobotIntentPhase.Acquisition:
            case RobotIntentPhase.Drop:
                robotTarget.transform.SetPositionAndRotation(AcquisitionBasePosition, AcquisitionBaseRotation);
                innerTargetIK.transform.SetPositionAndRotation(AcquisitionEndEffectorPosition, AcquisitionEndEffectorRotation);
                UndisplayRobotPreview();

                preAcquisitionPosition = robotCurrent.transform.position;
                preAcquisitionRotation = robotCurrent.transform.rotation;

                SetNewStateAndIntentPhase(RobotState.StowingToCurrentHeight, RobotIntentPhase.Acquisition);
                robotROS.FollowJointTrajectoryToCustom(stretchKinematicChain.GetLift(), 0f, 3.0f, -0.62f, 0.0f, 0f);
                break;
        }

    }

    #endregion

    #region "editor invocations"

    public void EnterRotation()
    {
        SetNewState(RobotState.CorrectiveRotating);
        gameObject.AddComponent<RobotRotator>();
    }


    #endregion


}

