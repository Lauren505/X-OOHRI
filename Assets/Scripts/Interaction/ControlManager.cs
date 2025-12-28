using Oculus.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum InputFeature
{
    // Debug
    ToggleDevMenu,
    // Send robot commands
    ToggleSendCommandsButton,
    SendCommands,
    // Interaction
    OpenRadialMenu,
    OpenExtendedMenu,
    UnselectEverything,
    StartLasso,
    ShowExplDuringMove
}

[Serializable]
public struct ControllerRawButtonMapping
{
    public OVRInput.RawButton button;
    public InputFeature inputFeature;
}

[Serializable]
public struct ControllerRawAxis2DMapping
{
    public OVRInput.RawAxis2D axis2D;
    public InputFeature inputFeature;
}

public class ControlManager : MonoBehaviour
{
    #region Controller settings
    public List<ControllerRawButtonMapping> rawButtonMapping;
    public List<ControllerRawAxis2DMapping> axis2DMapping;
    private Dictionary<InputFeature, OVRInput.RawButton> currentRawButtonMapping = new();
    private Dictionary<InputFeature, OVRInput.RawAxis2D> currentRawAxis2DMapping = new();
    public GameObject leftControllerRay;
    public RayInteractor rightControllerRay;
    #endregion

    public DebugCanvas debugCanvas;
    [SerializeField] private bool DevMode;
    [SerializeField] private bool TestNavMode = false;

    #region XObject
    public HashSet<XObject> allXObjects = new HashSet<XObject>();
    public HashSet<XObject> selectedXObjects = new HashSet<XObject>();
    public HashSet<XObject> movingXObjects = new HashSet<XObject>();
    public List<string> currentActionList = new List<string>();
    public bool IsAnyInResolution = false;
    [SerializeField]
    private List<XObject> selected = new List<XObject>();
    [SerializeField]
    private List<XObject> moving = new List<XObject>();
    #endregion

    public List<XObject> robotCommandObjects = new List<XObject>();
    public int currentCommandObject = 0;
    public RobotROS robotROS;

    private void Awake()
    {
        allXObjects.UnionWith(FindObjectsOfType<XObject>());
        SetControllerKeysConfig();
    }
    void Update()
    {
        if (OVRInput.GetDown(GetRawButton(InputFeature.ToggleDevMenu)))
        {
            DevMode = !DevMode;
            debugCanvas.ToggleMenu();
        }
        if (OVRInput.GetDown(GetRawButton(InputFeature.ShowExplDuringMove)) && selectedXObjects.Count == 1)
        {
            XObject selected = selectedXObjects.First();
            selected.constraintLabel.ToggleTooltip();
        }

        if(OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickUp))
        {
            NextCommandObj();
        }
        if(OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickDown))
        {
            PrevCommandObj();
        }
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick))
        {
            robotROS.FollowJointTrajectoryToStow();
        }

        UpdateCurrentActionSet();
        // Show in Inspector for debugging 
        selected = selectedXObjects.ToList();
        moving = movingXObjects.ToList();
    }
    #region Controller
    public OVRInput.RawButton GetRawButton(InputFeature inputFeature)
    {
        return currentRawButtonMapping[inputFeature];
    }
    public OVRInput.RawAxis2D GetAxis2D(InputFeature inputFeature)
    {
        return currentRawAxis2DMapping[inputFeature];
    }
    private void SetControllerKeysConfig()
    {
        foreach (ControllerRawButtonMapping mapping in rawButtonMapping)
        {
            currentRawButtonMapping[mapping.inputFeature] = mapping.button;
        }

        foreach (ControllerRawAxis2DMapping mapping in axis2DMapping)
        {
            currentRawAxis2DMapping[mapping.inputFeature] = mapping.axis2D;
        }

    }
    #endregion

    #region XObject helpers
    public void SetMovingTarget(XObject xobject)
    {
        movingXObjects.Add(xobject);
    }
    public void ClearMovingTarget()
    {
        movingXObjects.Clear();
    }
    public void SetMovingTargetAsSelected()
    {
        movingXObjects = selectedXObjects;
    }
    public void SetSelectedTarget(XObject xobject)
    {
        if (!selectedXObjects.Contains(xobject))
        {
            ClearSelectedTarget();
            selectedXObjects.Add(xobject);
            xobject.IsSelected = true;
        }
    }
    public void AddToSelectedTarget(XObject xobject)
    {
        selectedXObjects.Add(xobject);
        xobject.IsSelected = true;
    }
    public void RemoveFromSelectedTarget(XObject xobject)
    {
        selectedXObjects.Remove(xobject);
        xobject.IsSelected = false;
    }
    public void ClearSelectedTarget()
    {
        foreach (XObject xobject in selectedXObjects)
        { 
            xobject.IsSelected = false; 
            if (xobject.constraintLabel.InResolution)
            {
                xobject.constraintLabel.HideLabel();
                xobject.constraintLabel.ToggleTooltip();
            }
        }
        selectedXObjects.Clear();
    }
    public void HideAllXObjects()
    {
        foreach (XObject xobject in allXObjects)
        {
            xobject.HideMesh();
        }
    }
    public void HideAllExceptActiveXObjects()
    {
        foreach (XObject xobject in allXObjects)
        {
            xobject.InDebug = false;
        }
    }
    public void ShowAllXObjects()
    {
        foreach (XObject xobject in allXObjects)
        {
            xobject.InDebug = true;
        }
    }
    public void UnsetKinematicForAllXObjects()
    {
        foreach (XObject xobject in allXObjects)
        {
            xobject.UnsetKinematic();
        }
    }
    public void SetKinematicForAllXObjects()
    {
        foreach (XObject xobject in allXObjects)
        {
            xobject.SetKinematic();
        }
    }
    public void UpdateOriginalPoseForAll()
    {
        foreach (XObject xobject in allXObjects)
        {
            xobject.SetOriginalPose(xobject.ghost.position, xobject.ghost.rotation);
        }
    }
    public Vector3 GetCentroidOfGroup()
    {
        Vector3 centroid = new Vector3();
        foreach (XObject groupedXobject in selectedXObjects)
        {
            centroid += groupedXobject.OriginalPos;
        }
        return centroid / selectedXObjects.Count;
    }
    private void UpdateCurrentActionSet()
    {
        List<string> resolutionList = new List<string> { "Auto", "Alternative", "Ignore" };
        if (selectedXObjects.Count == 1)
        {
            if (selectedXObjects.First().constraintLabel.InResolution)
            {
                currentActionList = resolutionList;
                IsAnyInResolution = true;
            }
            else
            {
                currentActionList = selectedXObjects.First().actions;
                IsAnyInResolution = false;
            }
        }
        else if (selectedXObjects.Count > 1)
        {
            //HashSet<string> intersectionSet = new HashSet<string>(selectedXObjects.First().actions);
            var first = selectedXObjects.First();
            IEnumerable<string> possibleActions = first.constraintLabel.InResolution ? resolutionList : first.actions;
            foreach (XObject xobject in selectedXObjects.Skip(1))
            {
                if (xobject.constraintLabel.InResolution)
                {
                    possibleActions = resolutionList;
                    IsAnyInResolution = true;
                    break;
                }
                else
                { 
                    possibleActions = possibleActions.Intersect(xobject.actions);
                    IsAnyInResolution = false;
                }
            }
            currentActionList = possibleActions.ToList();
        }
        else
        {
            currentActionList = new List<string>();
            IsAnyInResolution = false;
        }
    }
    public void ForceResolutionState(List<XObject> xobjects)
    {
        foreach (XObject xobject in xobjects)
        {
            xobject.constraintLabel.InResolution = true;
        }
    }
    #endregion

    public bool GetDevModeState()
    { return DevMode; }
    public bool GetTestNavModeState()
    { return TestNavMode; }

    public void SetTestNavMode()
    {
        TestNavMode = !TestNavMode;
        if (TestNavMode)
        {
            leftControllerRay.SetActive(false);
        }
        else
        {
            leftControllerRay.SetActive(true);
        }
    }
    public void NextCommandObj()
    {
        if (currentCommandObject != robotCommandObjects.Count - 1)
        {
            currentCommandObject++;
        }
    }
    public void PrevCommandObj()
    {
        if (currentCommandObject != 0)
        {
            currentCommandObject--;
        }
    }
}
