using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SnapFeature : MonoBehaviour
{
    [Header("Snapping")]
    public float snapDistance = 0.1f;
    public float defaultDistance = 0.4f;
    private string snapDest = null;
    private bool defaultState = false;
    private Vector3 offset = Vector3.zero;
    private int currentSnapIndex = 0;
    private float snapTime = 0f;
    private bool animStart = false;
    private bool anim1 = false;
    private bool anim2 = false;
    private bool anim3 = false;
    private float timerStart;
    private bool hasMoved = false;

    [Header("Lines")]
    public GameObject trajectoryPrefab;
    public GameObject linePrefab;

    public Transform memSnapPos;
    private LineRenderer memTrajectoryLine;
    private LineRenderer twinConnectorLine;
    private Vector3[] memTrajectoryPoints;
    private int lineResolution = 30;

    [Header("Ghost")]
    public Transform ghostPos;
    private Vector3 originalGhostPos;
    private Rigidbody ghostRb;
    private GameObject basket;

    [Header("helper")]
    private XObject xobject;
    private ControlManager controlManager;
    private ActionHandler actionHandler;
    private Oculus.Interaction.GhostRayInteraction rayInteraction;

    void Start()
    {
        // Establish Children
        ghostRb = ghostPos.GetComponent<Rigidbody>();
        xobject = GetComponent<XObject>();
        controlManager = GameObject.Find("InteractionManager").GetComponent<ControlManager>();
        actionHandler = GameObject.Find("ActionHandler").GetComponent<ActionHandler>();
        rayInteraction = GetComponent<Oculus.Interaction.GhostRayInteraction>();
        originalGhostPos = xobject.OriginalPos;
        // Initialize Lines
        memTrajectoryLine = GameObject.Find("memTrajectory").GetComponent<LineRenderer>();
        lineResolution = memTrajectoryLine.positionCount;
        memTrajectoryPoints = new Vector3[lineResolution];
        memTrajectoryLine.useWorldSpace = true;
        DrawParabolicTrajectory(memTrajectoryLine, memTrajectoryPoints, transform.position, memSnapPos.position);
        //InitializeLines();
        basket = GameObject.Find("GhostBasket");
    }

    void Update()
    {
        if (animStart) return;
        // Group move
        if (controlManager.selectedXObjects.Count > 1)
        {
            if (rayInteraction.IsMoving()) //operating
            {
                offset = ghostPos.position - transform.position;
                CheckNearOriginalPosition(ghostPos);
                hasMoved = true;
                if (defaultState)
                {
                    DrawParabolicTrajectory(memTrajectoryLine, memTrajectoryPoints, transform.position, memSnapPos.position);
                    memTrajectoryLine.enabled = true;
                    SetColorNearTrajectory(ghostPos);
                    if (snapDest == "memory")
                    {
                        GroupMove(moving: true, relative: true, offset);
                    }
                    else
                    {
                        GroupMove(moving: true, relative: false, offset);
                    }
                }
                else
                {
                    memTrajectoryLine.enabled = false;
                    GroupMove(moving: true, relative: false, offset);
                }
            }
            else if (hasMoved)
            {
                if (snapDest == "memory")
                {
                    ghostPos.position = memSnapPos.position;
                    ghostPos.rotation = memSnapPos.rotation;
                    GroupMove(moving: false, relative: true);
                }
                GroupMove(moving: false, relative: false);
                hasMoved = false;
            }
            else
            {
                memTrajectoryLine.enabled = false;
            }
        }

        // Single move
        CheckNearOriginalPosition(ghostPos);
        if (defaultState)
        {
            if (rayInteraction.IsMoving())
            {
                DrawParabolicTrajectory(memTrajectoryLine, memTrajectoryPoints, transform.position, memSnapPos.position);
                memTrajectoryLine.enabled = true;
                SetColorNearTrajectory(ghostPos);
            }
            else
            {
                memTrajectoryLine.enabled = false;
                if (snapDest == "memory")
                {
                    ghostPos.position = memSnapPos.position;
                    ghostPos.rotation = memSnapPos.rotation;
                }
            }
        }
        else
        {
            memTrajectoryLine.enabled = false;
        }
    }

    public void ForceResolutionforFoamsBasket()
    {
        List<XObject> objectsInResolution = new List<XObject>(controlManager.selectedXObjects);
        objectsInResolution.Add(basket.GetComponent<XObject>());
        controlManager.ForceResolutionState(objectsInResolution);
    }
    public void AutoSnapAnim()
    {
        if (!animStart)
        {
            timerStart = Time.time;
            animStart = true;
            XObject xbasket = basket.GetComponent<XObject>();
            xbasket.ShowMesh();
            xbasket.SetColor();
            actionHandler.UpdatePickPose(xbasket);
            foreach (XObject xobject in controlManager.selectedXObjects)
            {
                xobject.SetColor();
                xobject.ghost.position = xobject.transform.position;
                actionHandler.UpdatePickPose(xobject);
                xobject.HideMesh();
                xobject.constraintLabel.HideLabel();
            }
        }
        else if (!anim1)
        {
            RunBasketLerp();
        }
        else if (!anim2)
        {
            RunFoamLerp();
        }
        else if (!anim3)
        {
            RunReturnLerp();
        }
    }
    private void RunBasketLerp()
    {
        XObject xbasket = basket.GetComponent<XObject>();
        Transform basketGhost = basket.transform.Find("GhostMesh");
        Transform basketMem = basket.transform.Find("memSnapPoint");
        
        if (Time.time - timerStart > 0.5f)
        {
            Vector3 start = basket.transform.position;
            Vector3 end = basketMem.position;
            snapTime += Time.deltaTime / 1f;
            basketGhost.position = Vector3.Lerp(start, end, snapTime);
            if (snapTime >= 1f)
            {
                DrawParabolicTrajectory(memTrajectoryLine, memTrajectoryPoints, transform.position, basketMem.position);
                basketGhost.position = basketMem.position;
                actionHandler.UpdatePlacePose(xbasket);
                foreach (XObject xobject in controlManager.selectedXObjects)
                {
                    xobject.ShowMesh();
                    xobject.SetKinematic();
                }
                memTrajectoryLine.enabled = true;
                snapTime = 0f;
                timerStart = Time.time;
                anim1 = true;
            }
        }
    }
    private void RunFoamLerp()
    {
        
        if (Time.time - timerStart > 0.5f)
        {
            memTrajectoryLine.enabled = true;
            Vector3 start = memTrajectoryPoints[currentSnapIndex];
            Vector3 end = memTrajectoryPoints[currentSnapIndex + 1];
            float segLength = Vector3.Distance(start, end);
            snapTime += (1.5f / segLength) * Time.deltaTime;
            
            foreach (XObject groupedXobject in controlManager.selectedXObjects)
            {
                Vector3 relativeOffset = groupedXobject.OriginalPos - controlManager.GetCentroidOfGroup();
                groupedXobject.ghost.position = Vector3.Lerp(start + relativeOffset, end + relativeOffset, snapTime);
            }
            if (snapTime >= 1f)
            {
                currentSnapIndex++;
                snapTime = 0f;
            }
            Vector3 endPos = memTrajectoryPoints[lineResolution - 1] + xobject.OriginalPos - controlManager.GetCentroidOfGroup();
            if (ghostPos.position == endPos)
            {
                foreach (XObject groupedXobject in controlManager.selectedXObjects)
                {
                    actionHandler.UpdatePlacePose(groupedXobject);
                }
                memTrajectoryLine.enabled = false;
                timerStart = Time.time;
                anim2 = true;
            }
        }
    }
    private void RunReturnLerp()
    {
        Transform basketGhost = basket.transform.Find("GhostMesh");
        Transform basketMem = basket.transform.Find("memSnapPoint");
        if (Time.time - timerStart > 0.5f)
        {
            Vector3 start = basketMem.position;
            Vector3 end = basket.transform.position;
            snapTime += Time.deltaTime / 1f;
            basketGhost.position = Vector3.Lerp(start, end, snapTime);
            foreach (XObject groupedXobject in controlManager.selectedXObjects)
            {
                Vector3 relativeOffset = groupedXobject.OriginalPos - controlManager.GetCentroidOfGroup();
                groupedXobject.ghost.position = Vector3.Lerp(start + relativeOffset, end + relativeOffset, snapTime);
            }
            if (snapTime >= 1f)
            {
                basketGhost.position = basket.GetComponent<XObject>().OriginalPos;
                snapTime = 0f;
                timerStart = Time.time;
                anim3 = true;
            }
        }
    }

    public void AutoSnapColored()
    {
        ghostPos.position = memSnapPos.position;
        ghostPos.rotation = memSnapPos.rotation;
        ghostRb.useGravity = true;
        ghostRb.isKinematic = false;
        memTrajectoryLine.startColor = new Color(0, 0.5058824f, 0.9843137f, 0f);
        memTrajectoryLine.endColor = new Color(0, 0.5058824f, 0.9843137f, 1f);
        memTrajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
        memTrajectoryLine.SetWidth(0.005f, 0.005f);
    }

    private void GroupMove(bool moving, bool relative, Vector3 offset = default)
    {
        foreach (XObject selectedObject in controlManager.selectedXObjects.ToList())
        {
            Transform selectedMemSnapPos = selectedObject.transform.Find("memSnapPoint");
            
            Vector3 relativeOffset = relative ? (offset.magnitude * (selectedMemSnapPos.position - selectedObject.ghost.position).normalized) : offset;
            if (selectedObject.Name != xobject.Name)
            {
                if (moving)
                {
                    selectedObject.SetKinematic();
                    selectedObject.ghost.position = selectedObject.transform.position + relativeOffset;
                    if (relative) selectedObject.ghost.rotation = selectedMemSnapPos.rotation;
                    else selectedObject.ghost.localRotation = Quaternion.identity;
                }
                else
                {
                    if (relative)
                    {
                        selectedObject.ghost.position = selectedMemSnapPos.position;
                        selectedObject.ghost.rotation = selectedMemSnapPos.rotation;
                    }
                    selectedObject.UnsetKinematic();
                }
            }
        }
    }

    public void InitializeLines()
    {
        // Create mem trajectory
        if (memTrajectoryLine != null)
        {
            Destroy(memTrajectoryLine);
        }
        GameObject memTrajectory = Instantiate(trajectoryPrefab, memSnapPos.transform.position, memSnapPos.transform.rotation);
        memTrajectory.transform.SetParent(transform, true);
        memTrajectory.name = "memTrajectory";
        memTrajectoryLine = memTrajectory.GetComponent<LineRenderer>();
        memTrajectoryLine.material.SetColor("_Color", Color.grey);
        lineResolution = memTrajectoryLine.positionCount;
        memTrajectoryPoints = new Vector3[lineResolution];
        DrawParabolicTrajectory(memTrajectoryLine, memTrajectoryPoints, originalGhostPos, memSnapPos.transform.position);

        // Create twin line
        if (twinConnectorLine != null)
        {
            Destroy(twinConnectorLine);
        }
        GameObject twinConnector = Instantiate(linePrefab, transform.position, transform.rotation);
        twinConnector.transform.SetParent(transform, true);
        twinConnector.name = "twinConnector";
        twinConnectorLine = twinConnector.GetComponent<LineRenderer>();
        DrawTwinLine(twinConnectorLine, transform, ghostPos);
        twinConnectorLine.enabled = false;
    }

    private void DrawTwinLine(LineRenderer line, Transform objectTransform, Transform targetTransform)
    {
        Vector3 startPos = objectTransform.position;
        Vector3 endPos = targetTransform.position;

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    private void DrawParabolicTrajectory(LineRenderer line, Vector3[] trajectoryPoints, Vector3 startPos, Vector3 endPos)
    {
        float trajectoryHeight = 0.15f;

        for (int i = 0; i < lineResolution; i++)
        {
            float t = i / (float)(lineResolution - 1);

            Vector3 point = CalculateParabola(startPos, endPos, trajectoryHeight, t);

            line.SetPosition(i, point);
            trajectoryPoints[i] = point;
        }
    }

    Vector3 CalculateParabola(Vector3 start, Vector3 end, float height, float t)
    {
        Vector3 midPoint = Vector3.Lerp(start, end, t);
        midPoint.y += Mathf.Sin(t * Mathf.PI) * height;

        return midPoint;
    }

    private void CheckNearOriginalPosition(Transform objectTransform)
    {
        float dist = Vector3.Distance(objectTransform.position, transform.position);
        if (dist <= defaultDistance && dist > 0.03f)
        {
            defaultState = true;
            return;
        }
        defaultState = false;
        snapDest = null;
    }

    private void SetColorNearTrajectory(Transform objectTransform)
    {
        bool isNearMem = false;

        float shortestMemDist = snapDistance;
        float dist;

        foreach (Vector3 point in memTrajectoryPoints)
        {
            dist = Vector3.Distance(objectTransform.position, point);
            if (dist <= snapDistance)
            {
                isNearMem = true;
                shortestMemDist = Mathf.Min(dist, shortestMemDist);
            }
        }
        if (isNearMem)
        {
            memTrajectoryLine.material.SetColor("_Color", Color.white);
            snapDest = "memory";
        }
        else
        {
            memTrajectoryLine.material.SetColor("_Color", Color.grey);
            snapDest = null;
        }
    }

}

