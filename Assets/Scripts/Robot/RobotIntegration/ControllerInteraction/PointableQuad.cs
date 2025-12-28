using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using System;
using System.ComponentModel.Design;

public class PointableQuad : MonoBehaviour, IPointableElement
{
    private bool isHovering = false;
    private bool isSelecting = false;

    private Skynet skynet;
    private ControlManager controlManager;
    private Quaternion initialControllerRotation;

    private float eps = 0.1f;

    void Start()
    {
        skynet = GameObject.Find("Skynet").GetComponent<Skynet>();
        controlManager = GameObject.Find("InteractionManager").GetComponent<ControlManager>();
    }

    public event Action<PointerEvent> WhenPointerEventRaised;

    private Pose mostRecentPose = new Pose(Vector3.zero, Quaternion.identity);

    public void ProcessPointerEvent(PointerEvent evt)
    {
        if (!controlManager.GetTestNavModeState())
        { return; }

        var interactor = evt.Data as RayInteractor;
         //Debug.Log($"interactorData: {interactor.CollisionInfo.Value.Point}");
        // Debug.Log($"eventData: {evt.Pose.position}");

        if (evt.Type == PointerEventType.Hover)
        {
            isHovering = true;

            Debug.Assert(mostRecentPose != null);
            Debug.Assert(mostRecentPose.position != null);
            skynet.NavigationCandidatePositionSelected(evt.Pose.position);
            Debug.Log("Navigation goal hovering at: " + evt.Pose.position);
        }
        else if (evt.Type == PointerEventType.Unhover)
        {
            isHovering = false;
            if (!isSelecting)
            {
                skynet.NavigationCandidateDiscarded();
            }
        }
        else if (evt.Type == PointerEventType.Select)
        {
            isSelecting = true;

            skynet.NavigationCandidatePositionSelected(evt.Pose.position);
            skynet.NavigationCandidateRotationSelected(Quaternion.identity);
            initialControllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RHand);
            Debug.Log("Navigate to: " + evt.Pose.position);
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            isSelecting = false;
            skynet.NavigationTargetSelected();
        }
        else if (evt.Type == PointerEventType.Move)
        {
            skynet.NavigationCandidatePositionSelected(evt.Pose.position);

            if (isSelecting)
            {
                var diffRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Quaternion.Inverse(initialControllerRotation);
                var yOnlyDiffRotation = Quaternion.Euler(0, diffRotation.eulerAngles.y, 0);
                skynet.NavigationCandidateRotationSelected(yOnlyDiffRotation);
            }
        }
    }
}
