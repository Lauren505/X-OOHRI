using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.Extensions;

public class ActionHandler : MonoBehaviour
{
    #region Action Handling
    public GameObject robotCurrent;
    public GameObject robotTarget;
    public GameObject robotCalib;
    public NavMeshRuntimeVisualizer navArea;
    public Transform roomTf;
    public GlobalEndEffectorKinematics globalEndEffectorKinematics;
    public ControlManager controlManager;

    private HashSet<XObject> movingObjects = new HashSet<XObject>();
    private HashSet<XObject> selectedObjects = new HashSet<XObject>();
    private bool targetIsPicked = false;
    private Pose pickPose;
    private Pose placePose;
    private XStretchRobot xStretchRobot;
    private List<int> errorCodes = new List<int>();
    #endregion

    void Awake()
    {
        xStretchRobot = robotCurrent.GetComponent<XStretchRobot>();
        xStretchRobot.Init(navArea);
    }
    void Update()
    {
        movingObjects = controlManager.movingXObjects;
        selectedObjects = controlManager.selectedXObjects;
        if (selectedObjects.Count == 0) return;

        foreach (XObject movingObject in movingObjects)
        {
            if (!movingObject.ActionSpecified && !movingObject.constraintLabel.InResolution)
            {
                List<(int ind, Pose end)> candidates = GetBaseCandidates(movingObject);

                if (PickObject(movingObject, candidates)) 
                { 
                    movingObject.ActionSpecified = true; 
                    if (selectedObjects.Count > 1)
                    {
                        foreach (XObject groupedObject in selectedObjects)
                        {
                            groupedObject.ActionSpecified = true;
                        }
                    }
                }
                else movingObject.ActionSpecified = false;
            }
        }
        foreach (XObject selectedObject in selectedObjects)
        {
            if (selectedObject.ActionSpecified && !selectedObject.constraintLabel.InResolution)
            {
                List<(int ind, Pose end)> candidates = GetBaseCandidates(selectedObject);
                bool canPlace = PlaceObject(selectedObject, candidates);
            }
        }
    }
    private List<(int ind, Pose end)> GetBaseCandidates(XObject xobject)
    {
        List<(int ind, Pose end)> candidates = new List<(int ind, Pose end)>();
        float baseOrientation = 0f;
        float extension = 0.075f;

        int gripperCount = xobject.GetEndEffectorTf().Count;
        for (int i = 0; i < gripperCount; i++)
        {
            Transform end = xobject.GetEndEffectorTf()[i];
            Pose basePose = globalEndEffectorKinematics.GetBestBasePoseFromEndEffectorRequest(end.position, end.rotation, baseOrientation, extension); 
            candidates.Add((i, basePose));
        }
        return candidates;
    }
    private bool PickObject(XObject xobject, List<(int ind, Pose end)> baseCandidates)
    {
        foreach ((int ind, Pose end) cand in baseCandidates)
        {
            errorCodes.Clear();
            errorCodes = xStretchRobot.Pick(xobject, cand.end.position);
            if (errorCodes.Count == 0)
            {
                xobject.SetColor();
                xobject.constraintLabel.HideLabel();
                xobject.PickBasePose = cand.end;
                xobject.PickGripper = new Pose(xobject.GetGripperTf()[cand.ind].position, xobject.GetGripperTf()[cand.ind].rotation);
                xobject.GripperInd = cand.ind;
                return true;
            }
            else
            {
                string detailParam = controlManager.selectedXObjects.Count > 1 ? xobject.GetClassName() : xobject.Name;
                xobject.SetColor(Color.red);
                if (errorCodes.Contains(1))
                {
                    xobject.constraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Size));
                    xobject.constraintLabel.SetTooltipText(ExplanationMapping.Detail(ExplanationTag.Size, detailParam));
                }
                else if (errorCodes.Contains(2))
                {
                    xobject.constraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Weight));
                    xobject.constraintLabel.SetTooltipText(ExplanationMapping.Detail(ExplanationTag.Weight, detailParam));
                }
                else if (errorCodes.Contains(3))
                {
                    xobject.constraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Height));
                    xobject.constraintLabel.SetTooltipText(ExplanationMapping.Detail(ExplanationTag.Height, detailParam));
                }
                else if (errorCodes.Contains(4))
                {
                    xobject.constraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Orientation));
                    xobject.constraintLabel.SetTooltipText(ExplanationMapping.Detail(ExplanationTag.Orientation, detailParam));
                }
                xobject.constraintLabel.ShowLabel();
            }
        }
        return false;
    }

    private bool PlaceObject(XObject xobject, List<(int ind, Pose end)> baseCandidates)
    {
        (int ind, Pose end) cand = baseCandidates[xobject.GripperInd];
        errorCodes.Clear();
        errorCodes = xStretchRobot.Place(xobject, cand.end.position);
        if (errorCodes.Count == 0)
        {
            xobject.SetColor();
            xobject.constraintLabel.HideLabel();
            xobject.PlaceBasePose = cand.end;
            xobject.PlaceGripper = new Pose(xobject.GetGripperTf()[cand.ind].position, xobject.GetGripperTf()[cand.ind].rotation);
            return true;
        }
        else
        {
            string detailParam = controlManager.selectedXObjects.Count > 1 ? xobject.GetClassName() : xobject.Name;
            xobject.SetColor(Color.red);
            if (errorCodes.Contains(5))
            {
                xobject.constraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Occlusion));
                xobject.constraintLabel.SetTooltipText(ExplanationMapping.Detail(ExplanationTag.Occlusion, detailParam));
            }
            else if (errorCodes.Contains(3))
            {
                xobject.constraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Height));
                xobject.constraintLabel.SetTooltipText(ExplanationMapping.Detail(ExplanationTag.Height, detailParam));
            }
            else if (errorCodes.Contains(4))
            {
                xobject.constraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Orientation));
                xobject.constraintLabel.SetTooltipText(ExplanationMapping.Detail(ExplanationTag.Orientation, detailParam));
            }
            xobject.constraintLabel.ShowLabel();
        }
        return false;
    }
    private void ClearViz(XObject xobject)
    {
        xobject.SetColor();
        xobject.constraintLabel.HideLabel();
    }

    public void UpdatePickPose(XObject xobject)
    {
        List<(int ind, Pose end)> candidates = GetBaseCandidates(xobject);
        foreach ((int ind, Pose end) cand in candidates)
        {
            errorCodes.Clear();
            errorCodes = xStretchRobot.Grab(xobject, cand.end.position);
            if (errorCodes.Count == 0)
            {
                xobject.PickBasePose = cand.end;
                xobject.PickGripper = new Pose(xobject.GetGripperTf()[cand.ind].position, xobject.GetGripperTf()[cand.ind].rotation);
                xobject.GripperInd = cand.ind;
                break;
            }
        }
    }
    public void UpdatePlacePose(XObject xobject)
    {
        List<(int ind, Pose end)> candidates = GetBaseCandidates(xobject);
        (int ind, Pose end) cand = candidates[xobject.GripperInd];
        errorCodes.Clear();
        errorCodes = xStretchRobot.Grab(xobject, cand.end.position);
        if (errorCodes.Count == 0)
        {
            xobject.PlaceBasePose = cand.end;
            xobject.PlaceGripper = new Pose(xobject.GetGripperTf()[cand.ind].position, xobject.GetGripperTf()[cand.ind].rotation);
        }
    }
}
