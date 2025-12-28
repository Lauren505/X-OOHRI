using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public delegate void IKPoseUpdated(
    Vector3 basePosition,
    Quaternion baseRotation,
    Vector3 endEffectorPosition,
    Quaternion endEffectorRotation, 
    string jobName
    );
public delegate void GrabPoseUpdated(
    Vector3 basePosition,
    Quaternion baseRotation,
    Vector3 endEffectorPosition,
    Quaternion endEffectorRotation,
    Vector3 dropBasePosition,
    Quaternion dropBaseRotation,
    Vector3 dropEndEffectorPosition,
    Quaternion dropEndEffectorRotation,
    string jobName
    );

public class GlobalEndEffectorKinematics : MonoBehaviour
{
    public StretchKinematicChain stretchKinematicChain;
    public Transform endEffectorTransform;
    public OVRInput.Button acqDropButton;

    private Transform rightHandAnchor;
    private ReverseForwardKinematics reverseForwardKinematics;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    public IKPoseUpdated iKPoseUpdated;
    public GrabPoseUpdated grabPoseUpdated;

    public ControlManager controlManager;
    private Skynet skynet;

    public Vector3 basketOffset = new Vector3(0f, 0f, -0.2f);

    void Start()
    {
        rightHandAnchor = GameObject.Find("RightHandAnchor").transform;
        skynet = GameObject.Find("Skynet").GetComponent<Skynet>();

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        reverseForwardKinematics = stretchKinematicChain.GetComponent<ReverseForwardKinematics>();
    }


    public Pose GetBestBasePoseFromEndEffectorRequest(Vector3 position, Quaternion rotation, float baseOrientation, float extension)
    {
        var mGripper = Matrix4x4.TRS(position, rotation.normalized, Vector3.one);
        var mBestPose = reverseForwardKinematics.ComputeBaseTransformFromGripper(mGripper, reverseForwardKinematics.staticLinkTransforms, baseOrientation, extension);
        return new Pose(mBestPose.GetPosition(), mBestPose.rotation.normalized);
    }


    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            SendAcq();
        }
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            SendDrop();
        }
        
        
        if (
              OVRInput.GetDown(acqDropButton) ||
              OVRInput.Get(acqDropButton)
          )
        {
            transform.SetPositionAndRotation(rightHandAnchor.transform.position, rightHandAnchor.transform.rotation);
        }
        else if (OVRInput.GetUp(acqDropButton))
        {
            lastPosition = rightHandAnchor.transform.position;
            lastRotation = rightHandAnchor.transform.rotation;

            UpdateIKPose(lastPosition, lastRotation, "controllerInducedJob");
        }
    }
    public void SendAcq()
    {
        skynet.gripperAperture = 0.6f;
        XObject currentObject = controlManager.robotCommandObjects[controlManager.currentCommandObject];

        iKPoseUpdated(currentObject.PickBasePose.position, currentObject.PickBasePose.rotation, currentObject.PickGripper.position, currentObject.PickGripper.rotation, "debugPanelInducedJob");
        
        Debug.Log("Acq at: " + currentObject.PickBasePose.position + "  " + currentObject.PickGripper.position);
    }
    public void SendDrop()
    {
        skynet.gripperAperture = 0.6f;
        XObject currentObject = controlManager.robotCommandObjects[controlManager.currentCommandObject];
        iKPoseUpdated(currentObject.PlaceBasePose.position, currentObject.PlaceBasePose.rotation, currentObject.PlaceGripper.position, currentObject.PlaceGripper.rotation, "debugPanelInducedJob");
        Debug.Log("Drop at: " + currentObject.PlaceBasePose.position + "  " + currentObject.PlaceGripper.position);
    }

    public void UpdateIKPose(Vector3 pos, Quaternion rot, string jobName)
    {
        transform.SetPositionAndRotation(pos, rot);

        var bestBasePose = GetBestBasePoseFromEndEffectorRequest(endEffectorTransform.position, endEffectorTransform.rotation, 0f, 0.075f);
        iKPoseUpdated(bestBasePose.position, bestBasePose.rotation, pos, rot, jobName);
    }

    public void AddBasketX()
    {
        basketOffset.x += 0.01f;
    }
    public void DecBasketX()
    {
        basketOffset.x -= 0.01f;
    }
    public void AddBasketZ()
    {
        basketOffset.z += 0.01f;
    }
    public void DecBasketZ()
    {
        basketOffset.z -= 0.01f;
    }
}
