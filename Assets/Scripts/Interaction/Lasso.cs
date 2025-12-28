using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Lasso : MonoBehaviour
{
    [Header("params")]
    public LayerMask lassoLayer;
    public Transform rightHandTransform;
    public Transform headTransform;
    public GameObject lassoPrefab;
    public ControlManager controlManager;

    public GameObject roomba;
    
    public float LassoDistance = 5f;
    public bool hoverOverUI = false;
    
    public bool lassoIsDrawnFlag = false;
    private LayerMask currentLayer;
    private float startPressTime = 0f;

    // office scene only
    private Toggle labelButton;
    private bool coffeeTableStarted = false;
    public bool coffeeTableDone = false;

    [Header("line")]
    private LineRenderer objectLine;
    private LineRenderer areaLine;
    private int objectLineIndex = 0;
    private int areaLineIndex = 0;

    [Header("mesh")]
    private MeshCollider areaMeshCollider;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;

    [Header("Intersect")]
    private Vector3 firstImpactNormal;
    private HashSet<GameObject> lassoSelectedAnchors = new HashSet<GameObject>();
    public List<Collider> lassoSelectedGhosts = new List<Collider>(); // for area selection

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>(); 
        meshFilter = GetComponent<MeshFilter>(); 
    }

    void Update()
    {
        if (OVRInput.GetDown(controlManager.GetRawButton(InputFeature.StartLasso)))
        {
            startPressTime = Time.time;
        }
        if (OVRInput.Get(controlManager.GetRawButton(InputFeature.StartLasso)) && ((Time.time - startPressTime) > 0.2f))
        {
            ObjectDraw(lassoLayer);
        }
        else if (objectLine != null)
        {
            CreateLassoMesh(objectLine, area: false);
            CheckObjectsInsideMesh(area: false);

            Destroy(objectLine.gameObject);
            lassoSelectedAnchors.Clear();
        }
    }

    public void ForceClearArea(bool state)
    {
        if (areaLine != null)
        {
            ClearArea();
        }
    }

    public void ClearSelection(bool state)
    {
        //if (IM.areaButton.isOn)
        //{
        //    ClearArea();
        //}
        //else if (IM.objectButton.isOn)
        //{
        //    IM.DeselectAll();
        //}
    }

    public void ClearArea()
    {
        if (areaLine != null) Destroy(areaLine.gameObject);
        lassoSelectedAnchors.Clear();
        lassoSelectedGhosts.Clear();
        lassoIsDrawnFlag = false;
    }

    public void IsHoverOverUI()
    {
        hoverOverUI = true;
    }

    public void NotHoverOverUI()
    {
        hoverOverUI = false;
    }

    public Vector3 GetAreaStartPos()
    {
        if (areaLine != null)
        {
            return areaLine.GetPosition(0);
        }
        else return Vector3.zero;
    }

    private void ObjectDraw(LayerMask currentLayer)
    {
        Ray ray = new Ray(rightHandTransform.position, rightHandTransform.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, LassoDistance, currentLayer);
        Vector3 endPoint = Vector3.zero;

        if(hasHit && !hoverOverUI)
        {
            endPoint = hit.point;

            if (objectLine == null)
            {
                firstImpactNormal = hit.normal;
                Quaternion rayImpactRotation = Quaternion.LookRotation(firstImpactNormal);
                objectLineIndex = 0;
                objectLine = Instantiate(lassoPrefab, hit.point, rayImpactRotation).GetComponent<LineRenderer>();
                objectLine.transform.SetParent(transform);
                objectLine.positionCount = 1;
                objectLine.startColor = Color.white;
                objectLine.endColor = Color.white;
                objectLine.SetPosition(0, hit.point);
            }
            else
            {
                Vector3 currentPosition = objectLine.GetPosition(objectLineIndex);
                if (Vector3.Distance(currentPosition, hit.point) > 0.04f)
                {
                    objectLineIndex++;
                    objectLine.positionCount = objectLineIndex + 1;
                    objectLine.SetPosition(objectLineIndex, hit.point);
                }
            }
        }

    }

    private void AreaDraw(LayerMask currentLayer)
    {
        Ray ray = new Ray(rightHandTransform.position, rightHandTransform.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, LassoDistance, currentLayer);
        Vector3 endPoint = Vector3.zero;

        if(hasHit && !hoverOverUI)
        {
            endPoint = hit.point;

            if (areaLine == null)
            {
                firstImpactNormal = hit.normal;
                Quaternion rayImpactRotation = Quaternion.LookRotation(firstImpactNormal);
                areaLineIndex = 0;
                areaLine = Instantiate(lassoPrefab, hit.point, rayImpactRotation).GetComponent<LineRenderer>();
                areaLine.transform.SetParent(transform);
                areaLine.positionCount = 1;
                areaLine.startColor = Color.white;
                areaLine.endColor = Color.white;
                areaLine.SetWidth(0.075f, 0.075f);
                areaLine.SetPosition(0, hit.point);
            }
            else
            {
                Vector3 currentPosition = areaLine.GetPosition(areaLineIndex);
                if (Vector3.Distance(currentPosition, hit.point) > 0.04f)
                {
                    areaLineIndex++;
                    areaLine.positionCount = areaLineIndex + 1;
                    areaLine.SetPosition(areaLineIndex, hit.point);
                }
            }
        }
    }

    private void CreateLassoMesh(LineRenderer line, bool area)
    {
        if (line.positionCount < 3) return;

        Vector3[] lassoPoints = new Vector3[line.positionCount];
        line.GetPositions(lassoPoints);

        Mesh lassoMesh = CreateExtrudedMesh(lassoPoints);

        if (area)
        {
            if (areaMeshCollider == null) 
            {   
                areaMeshCollider = gameObject.AddComponent<MeshCollider>();
                areaMeshCollider.convex = true;
                areaMeshCollider.isTrigger = true;
            }
            areaMeshCollider.sharedMesh = lassoMesh;
        }
        else
        {
            meshCollider.sharedMesh = lassoMesh;
            meshFilter.mesh = lassoMesh;
        }
    }

    private Mesh CreateExtrudedMesh(Vector3[] basePoints)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int baseVertexCount = basePoints.Length;

        for (int i = 0; i < baseVertexCount; i++)
        {
            Vector3 bottom = basePoints[i] - firstImpactNormal * 0.1f;
            Vector3 top = bottom + firstImpactNormal * 0.5f;
            vertices.Add(bottom);
            vertices.Add(top);
        }

        for (int i = 0; i < baseVertexCount; i++)
        {
            int next = (i + 1) % baseVertexCount;

            int bottomA = i * 2;
            int bottomB = next * 2;
            int topA = bottomA + 1;
            int topB = bottomB + 1;

            // First Triangle
            triangles.Add(bottomA);
            triangles.Add(topA);
            triangles.Add(bottomB);

            // Second Triangle
            triangles.Add(topA);
            triangles.Add(topB);
            triangles.Add(bottomB);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void CheckObjectsInsideMesh(bool area)
    {
        Collider[] allColliders = FindObjectsOfType<Collider>();
        lassoSelectedAnchors.Clear();
        controlManager.ClearSelectedTarget();

        foreach (Collider col in allColliders)
        {
            if (col.gameObject.layer == 8)
            {
                if (IsColliderInMesh(col, area))
                {
                    lassoSelectedGhosts.Add(col);
                    XObject xobject = col.GetComponentInParent<XObject>();
                    xobject.IsSelected = true;
                    controlManager.AddToSelectedTarget(xobject);
                }
            }
        }
    }

    private bool IsColliderInMesh(Collider col, bool area)
    {
        if (area && areaMeshCollider != null)
        {
            return Physics.ComputePenetration(
                areaMeshCollider, areaMeshCollider.transform.position, areaMeshCollider.transform.rotation, 
                col, col.transform.position, col.transform.rotation,
                out Vector3 direction, out float distance
            );
        }
        else 
        {
            return Physics.ComputePenetration(
                meshCollider, meshCollider.transform.position, meshCollider.transform.rotation, 
                col, col.transform.position, col.transform.rotation,
                out Vector3 direction, out float distance
            );
        }
    }

    private bool IsConstraintResolved()
    {
        bool isConstraintResolved = true;

        if (lassoSelectedGhosts.Count > 0)
        {
            foreach (Collider ghost in lassoSelectedGhosts)
            {
                if (IsColliderInMesh(ghost, area: true))
                {
                    isConstraintResolved = false;
                }
            }
        }
        return isConstraintResolved;
    }

    private void UpdateLassoColor()
    {
        if (!IsConstraintResolved())
        {
            areaLine.startColor = Color.red;
            areaLine.endColor = Color.red;
        }
        else if (IsConstraintResolved() && lassoIsDrawnFlag)
        {
            areaLine.startColor = Color.white;
            areaLine.endColor = Color.white;
        }
    }
}
