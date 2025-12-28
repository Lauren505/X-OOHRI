using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReverseForwardKinematics : MonoBehaviour
{
    public Dictionary<string, Transform> linkTransforms;
    public List<string> linkNames = new List<string>
    {
        "link_mast",
        "link_lift_origin",
        "link_lift",
        "link_arm_l4",
        "link_arm_l3_origin",
        "link_arm_l3",
        "link_arm_l2_origin",
        "link_arm_l2",
        "link_arm_l1_origin",
        "link_arm_l1",
        "link_arm_l0_origin",
        "link_arm_l0",
        "link_wrist_yaw_origin",
        "link_wrist_yaw",
        "link_DW3_wrist_yaw_bottom",
        "link_DW3_wrist_pitch_origin",
        "link_DW3_wrist_pitch",
        "link_DW3_wrist_roll_origin",
        "link_DW3_wrist_roll",
        "link_SG3_gripper_body"
    };

    
    public GameObject coordinateFrame;
    public Dictionary<string, Matrix4x4> staticLinkTransforms;

    public bool animateReverseForwardKinematics;

    private Dictionary<string, Matrix4x4> ExtractLocalTransformsFromKinematicChain()
    {
        Dictionary<string, Matrix4x4> linkMatrices = new();
        foreach (var linkName in linkNames)
        {
            var t = linkTransforms[linkName];
            var m = Matrix4x4.TRS(t.localPosition, t.localRotation, Vector3.one);
            linkMatrices[linkName] = m;
        }

        return linkMatrices;
    }

    private Dictionary<string, Matrix4x4> GetSubsetOfStaticTransformsInKinematicChain()
    {
        var links = ExtractLocalTransformsFromKinematicChain();
        links.Remove("link_DW3_wrist_roll");
        links.Remove("link_DW3_wrist_pitch");
        links.Remove("link_wrist_yaw");
        links.Remove("link_arm_l0");
        links.Remove("link_arm_l1");
        links.Remove("link_arm_l2");
        links.Remove("link_arm_l3");
        links.Remove("link_lift");

        return links;
    }
   

    void Start()
    {
        linkTransforms = new Dictionary<string, Transform>();
        Transform currentTransform = transform;

        foreach (string linkName in linkNames)
        {
            currentTransform = currentTransform.Find(linkName);
            Debug.Assert(currentTransform != null);
            linkTransforms[linkName] = currentTransform;
        }

        staticLinkTransforms = GetSubsetOfStaticTransformsInKinematicChain();
    }

    public static float GetEndEffectorRollInRadFromGlobalRotation(Quaternion globalRotation)
    {
        globalRotation = globalRotation.normalized;
        var rotatedXAxis = (globalRotation * Vector3.right).normalized;
        var rotatedYAxis = (globalRotation * Vector3.up).normalized;
        var rotatedZAxis = (globalRotation * Vector3.forward).normalized;
        var vectorOrthogonalToUpwardPlane = Vector3.Cross(rotatedYAxis, Vector3.up).normalized;
        var dotProduct = Vector3.Dot(rotatedZAxis, vectorOrthogonalToUpwardPlane);

        if(dotProduct >= 1f || dotProduct <= -1f){
            return 0f;
        }

        float sign = (-1) * Mathf.Sign(Vector3.Dot(vectorOrthogonalToUpwardPlane, rotatedXAxis));

        float angleInRadians = Mathf.Acos(dotProduct);

        return angleInRadians * sign;
    }

    public static float GetEndEffectorPitchInRadFromGlobalRotation(Quaternion globalRotation)
    {
        globalRotation = globalRotation.normalized;
        var rotatedYAxis = (globalRotation * Vector3.up).normalized;
        var vectorOrthogonalToUpwardPlane = Vector3.Cross(rotatedYAxis, Vector3.up).normalized;
        var vectorHorizontalForward = Vector3.Cross(Vector3.up, vectorOrthogonalToUpwardPlane).normalized;
        var dotProduct = Vector3.Dot(vectorHorizontalForward, rotatedYAxis);

        if(dotProduct >= 1 || dotProduct <= -1){
            return 0f;
        }

        float sign = Mathf.Sign(Vector3.Dot(Vector3.up, rotatedYAxis));
        float angleInRadians = Mathf.Acos(dotProduct);
        float signedAngle = angleInRadians * sign; 

        return signedAngle;
    }


    public static Matrix4x4 GetEndEffectorRollMatrixFromGlobalRotation(Quaternion globalRotation)
    {
        var rollInDegrees = GetEndEffectorRollInRadFromGlobalRotation(globalRotation.normalized) * Mathf.Rad2Deg;
        var roll_matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, rollInDegrees, 0f), Vector3.one);
        return roll_matrix;
    }

    public static Matrix4x4 GetEndEffectorPitchMatrixFromGlobalRotation(Quaternion globalRotation)
    {
        var angleInDegrees = GetEndEffectorPitchInRadFromGlobalRotation(globalRotation.normalized) * Mathf.Rad2Deg;
        var angleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, angleInDegrees, 0f), Vector3.one);
        return angleMatrix;
    }

    public static float GetLiftFromL4Height(Vector3 l4Position)
    {
        var liftOffset = 0.2f; // figured out interactively in Unity by 1) pushing IK target to lowest, medium, and max height, and then 2) looking at Debug.Log($"l4Position: {l4Position.y}"); and at lift joint y translation
        var lift = l4Position.y - liftOffset;
        return lift;
    }

    public static Matrix4x4 GetLiftMatrixFromL4Height(Vector3 l4Position)
    {
        var lift = GetLiftFromL4Height(l4Position);
        return Matrix4x4.TRS(new Vector3(0f, lift, 0f), Quaternion.identity, Vector3.one);
    }


    public static Matrix4x4 ComputeBaseTransformFromHierarchy(Matrix4x4 mGripper, List<string> linkNames, Dictionary<string, Matrix4x4> linkMatrices)
    {
        var parent = mGripper;
        foreach (var linkName in linkNames.AsEnumerable().Reverse())
        {
            parent = parent * linkMatrices[linkName].inverse;
        }

        return parent;
    }

    public Matrix4x4 ComputeBaseTransformFromGripper(Matrix4x4 globalGripperTransform, Dictionary<string, Matrix4x4> staticLinkMatrices, float desiredYawInDegrees, float desiredSegmentExtension)
    {
        var armSegmentExtension = Matrix4x4.TRS(new Vector3(0f, desiredSegmentExtension, 0f), Quaternion.identity, Vector3.one);
        var mYaw = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, desiredYawInDegrees, 0f), Vector3.one); ;

        // derived angles
        var mPitch = GetEndEffectorPitchMatrixFromGlobalRotation(globalGripperTransform.rotation);
        var mRoll = GetEndEffectorRollMatrixFromGlobalRotation(globalGripperTransform.rotation);

        // toggle everything after a certain line here to see where the transform ends up
        var mDerivedArmL4 = globalGripperTransform
            * staticLinkMatrices["link_SG3_gripper_body"].inverse
            * mRoll.inverse
            * staticLinkMatrices["link_DW3_wrist_roll_origin"].inverse
            * mPitch.inverse
            * staticLinkMatrices["link_DW3_wrist_pitch_origin"].inverse
            * staticLinkMatrices["link_DW3_wrist_yaw_bottom"].inverse
            * mYaw.inverse
            * staticLinkMatrices["link_wrist_yaw_origin"].inverse
            * armSegmentExtension.inverse
            * staticLinkMatrices["link_arm_l0_origin"].inverse
            * armSegmentExtension.inverse
            * staticLinkMatrices["link_arm_l1_origin"].inverse
            * armSegmentExtension.inverse
            * staticLinkMatrices["link_arm_l2_origin"].inverse
            * armSegmentExtension.inverse
            * staticLinkMatrices["link_arm_l3_origin"].inverse
            * staticLinkMatrices["link_arm_l4"].inverse;

        var mLift = GetLiftMatrixFromL4Height(mDerivedArmL4.GetPosition());
        var mBase = mDerivedArmL4
            * mLift.inverse
            * staticLinkMatrices["link_lift_origin"].inverse
            * staticLinkMatrices["link_mast"].inverse;


        if(animateReverseForwardKinematics){
            var computationDict = new Dictionary<string, Matrix4x4>();
            var chain = globalGripperTransform;
            computationDict["globalGripperTransform"] = chain;
            
            chain = chain * staticLinkMatrices["link_SG3_gripper_body"].inverse;
            computationDict["link_SG3_gripper_body"] = chain;
            
            chain = chain * mRoll.inverse;
            computationDict["link_DW3_wrist_roll"] = chain;
            
            chain = chain * staticLinkMatrices["link_DW3_wrist_roll_origin"].inverse;
            computationDict["link_DW3_wrist_roll_origin"] = chain;
            
            chain = chain * mPitch.inverse;
            computationDict["link_DW3_wrist_pitch"] = chain;
            
            chain = chain * staticLinkMatrices["link_DW3_wrist_pitch_origin"].inverse;
            computationDict["link_DW3_wrist_pitch_origin"] = chain;
            
            chain = chain * staticLinkMatrices["link_DW3_wrist_yaw_bottom"].inverse;
            computationDict["link_DW3_wrist_yaw_bottom"] = chain;
            
            chain = chain * mYaw.inverse;
            computationDict["link_wrist_yaw"] = chain;
            
            chain = chain * staticLinkMatrices["link_wrist_yaw_origin"].inverse;
            computationDict["link_wrist_yaw_origin"] = chain;
            
            chain = chain * armSegmentExtension.inverse;
            computationDict["link_arm_l0"] = chain;
            
            chain = chain * staticLinkMatrices["link_arm_l0_origin"].inverse;
            computationDict["link_arm_l0_origin"] = chain;
            
            chain = chain * armSegmentExtension.inverse;
            computationDict["link_arm_l1"] = chain;
            
            chain = chain * staticLinkMatrices["link_arm_l1_origin"].inverse;
            computationDict["link_arm_l1_origin"] = chain;
            
            chain = chain * armSegmentExtension.inverse;
            computationDict["link_arm_l2"] = chain;
            
            chain = chain * staticLinkMatrices["link_arm_l2_origin"].inverse;
            computationDict["link_arm_l2_origin"] = chain;
            
            chain = chain * armSegmentExtension.inverse;
            computationDict["link_arm_l3"] = chain;
            
            chain = chain * staticLinkMatrices["link_arm_l3_origin"].inverse;
            computationDict["link_arm_l3_origin"] = chain;
            
            chain = chain * staticLinkMatrices["link_arm_l4"].inverse;
            computationDict["link_arm_l4"] = chain;
            
            chain = chain * mLift.inverse;
            computationDict["link_lift"] = chain;
            
            chain = chain * staticLinkMatrices["link_lift_origin"].inverse;
            computationDict["link_lift_origin"] = chain;
            
            chain = chain * staticLinkMatrices["link_mast"].inverse;
            computationDict["link_mast"] = chain;
        }
        return mBase;
    }



    public bool setCoordinateFrameToGripperTriggered;
    public bool setCoordinateFrameToBaseFromEndEffectorOnly;
    public bool setCoordinateFrameToBaseViaFullKinematicJoinKnowledge;

    void Update()
    {
        Dictionary<string, Matrix4x4> linkMatricesWithCurrentTransforms = ExtractLocalTransformsFromKinematicChain();

        var mGripper = Matrix4x4.TRS(linkTransforms["link_SG3_gripper_body"].position, linkTransforms["link_SG3_gripper_body"].rotation, Vector3.one);

        if (setCoordinateFrameToGripperTriggered)
        {
            setCoordinateFrameToGripperTriggered = false;
            coordinateFrame.transform.SetPositionAndRotation(mGripper.GetPosition(), mGripper.rotation);
        }

        if (setCoordinateFrameToBaseFromEndEffectorOnly)
        {
            setCoordinateFrameToBaseFromEndEffectorOnly = false;

            var mBase = ComputeBaseTransformFromGripper(mGripper, staticLinkTransforms, 0f, 0.075f);
            coordinateFrame.transform.SetPositionAndRotation(mBase.GetPosition(), mBase.rotation);
        }


        if (setCoordinateFrameToBaseViaFullKinematicJoinKnowledge)
        {
            setCoordinateFrameToBaseViaFullKinematicJoinKnowledge = false;
            var mBase = ComputeBaseTransformFromHierarchy(mGripper, linkNames, linkMatricesWithCurrentTransforms);
            coordinateFrame.transform.SetPositionAndRotation(mBase.GetPosition(), mBase.rotation);
        }

    }

  
}
