using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotRotator : MonoBehaviour
{

    private Skynet skynet;
    private RobotROS robotROS;
    private GameObject robotTarget;
    private GameObject robotCurrent;
    private IKInner ikInner;

    public RotationFinished rotationFinished;

    private long cooldownInMs = 100;
    private long lastCmdVelEpoch = 0;
    private float tolerance = 0.02f;
    private float maxAngularVelocity = 0.5f;
    // private float angularGain = 0.1f;

    public Vector3 targetPoint;
    private GameObject sphere;

    void Start()
    {
        skynet = GetComponent<Skynet>(); // as robot rotator is added by skynet, this should always work
        robotROS = skynet.robotROS;
        robotTarget = skynet.robotTarget;
        robotCurrent = skynet.robotCurrent;
        ikInner = skynet.robotTarget.GetComponentInChildren<IKInner>();

        targetPoint = ComputePointTarget();
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        sphere.transform.position = targetPoint;
        sphere.transform.localScale = Vector3.one * 0.1f;
    }

    Vector3 ComputePointTarget()
    {
        var globalTargetEndEffectorTransform = Matrix4x4.TRS(ikInner.transform.position, ikInner.transform.rotation, Vector3.one);
        var grasapbleObjectDistance = Vector3.forward * 0.2f;
        var globalTargetPoint = globalTargetEndEffectorTransform.MultiplyPoint(grasapbleObjectDistance);
        return globalTargetPoint; 
    }

    static Tuple<Vector3, Vector3, float> ProjectPointTargetOnUpwardPlane(Vector3 currentBasePosition, Vector3 currentReachVector, Vector3 targetPoint) {
        Vector3 planeOrigin = currentBasePosition;
        Vector3 planeNormal = Vector3.Cross(Vector3.up, currentReachVector).normalized;
        Vector3 point = targetPoint;

        Vector3 v = point - planeOrigin;
        Vector3 rescaledPlaneNormal = Vector3.Project(v, planeNormal.normalized);
        float planeNormalScalingFactor = Vector3.Dot(v, planeNormal.normalized);
        Vector3 projectedPoint = point - rescaledPlaneNormal;

        // Debug.Log($"planeOrigin {planeOrigin}, planeNormal {planeNormal}, point {point}, planeNormalScalingFactor: {planeNormalScalingFactor}");

        return new (projectedPoint, rescaledPlaneNormal, planeNormalScalingFactor);
    }


    float ComputeAngularOffsetInRadFromTargetPoint()
    {
        var basePose = Matrix4x4.TRS(robotCurrent.transform.position, robotCurrent.transform.rotation, Vector3.one);
        var currentReachVector = basePose * Vector3.right; // rough approximation due to Stretch's design
        var targetReachVector = targetPoint - robotCurrent.transform.position;

        var currentReachVectorInXz = new Vector3(currentReachVector.x, 0f, currentReachVector.z);
        var targetReachVectorInXz = new Vector3(targetReachVector.x, 0f, targetReachVector.z);

        var offsetAngle = Mathf.Acos(Vector3.Dot(currentReachVectorInXz.normalized, targetReachVectorInXz.normalized));
        var pointProjection = ProjectPointTargetOnUpwardPlane(robotCurrent.transform.position, currentReachVectorInXz, targetPoint);

        var signedOffsetAngle = (-1) * Mathf.Sign(pointProjection.Item3) * offsetAngle;
        signedOffsetAngle += Mathf.Deg2Rad * 3;

        // Debug.Log($"signedOffsetAngle: {signedOffsetAngle}");
        return signedOffsetAngle; 
    }


    void Update()
    {
        
        var angularOffset = ComputeAngularOffsetInRadFromTargetPoint();
        var closeEnough = Math.Abs(angularOffset) < tolerance;
        if (!closeEnough)
        {
            long currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (currentEpoch - lastCmdVelEpoch > cooldownInMs)
            {
                robotROS.SendCmdVelOneOff(0f, 0f, 0f, 0f, 0f, Math.Clamp(angularOffset, -maxAngularVelocity, maxAngularVelocity)); // proportional control
                lastCmdVelEpoch = currentEpoch;
            }
        } else
        {
            rotationFinished();
        }
    }

    private void OnDestroy()
    {
        Destroy(sphere);
    }
}
