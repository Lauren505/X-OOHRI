using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperManager : MonoBehaviour
{
    // replace this later by Skynet


    public GlobalEndEffectorKinematics globalEndEffectorKinematics_Acquisition;
    public GlobalEndEffectorKinematics globalEndEffectorKinematics_Drop;
    public GameObject robotTarget;
    public IKInner innerTargetIK;

    void Start()
    {
        globalEndEffectorKinematics_Acquisition.iKPoseUpdated += AcquisitionIKUpdated;
        globalEndEffectorKinematics_Drop.iKPoseUpdated += DropIKUpdated;
    }


    void Update()
    {
        
    }

    public void AcquisitionIKUpdated(
        Vector3 basePosition,
        Quaternion baseRotation,
        Vector3 endEffectorPosition,
        Quaternion endEffectorRotation,
        string jobName
    )
    {
        robotTarget.transform.SetPositionAndRotation(basePosition, baseRotation);
        innerTargetIK.transform.SetPositionAndRotation(endEffectorPosition, endEffectorRotation);
    }

    public void DropIKUpdated(
        Vector3 basePosition,
        Quaternion baseRotation,
        Vector3 endEffectorPosition,
        Quaternion endEffectorRotation,
        string jobName
    )
    {
        // robotTarget.transform.SetPositionAndRotation(AcquisitionBasePosition, AcquisitionBaseRotation);
        // innerTargetIK.transform.SetPositionAndRotation(AcquisitionEndEffectorPosition, AcquisitionEndEffectorRotation);
    }
}
