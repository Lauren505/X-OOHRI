
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Utilities.Extensions;

public interface IWalkableArea
{
    bool PointInNavMesh(Vector3 point);
    bool PointInHeightLimit(Vector3 point);
    bool PointOccluded(XObject xobject);
}


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class NavMeshRuntimeVisualizer : MonoBehaviour, IWalkableArea
{
    [Header("Navigation mesh")]
    public Transform roomTf;
    public GameObject floorPreview;
    private MeshRenderer mr;
    private MeshRenderer floormr;
    Mesh _mesh;

    [Header("Reachability")]
    public Collider unreachableHeight;

    void Awake()
    {
        _mesh = new Mesh { name = "NavMesh Visual" };
        GetComponent<MeshFilter>().mesh = _mesh;

        floormr = floorPreview.GetComponent<MeshRenderer>();
        mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial ??= new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(0, 1, 1, 0.3f),
            enableInstancing = true
        };

        Rebuild();
    }

    public void Rebuild()
    {
        var tri = NavMesh.CalculateTriangulation();
        _mesh.Clear();
        _mesh.vertices = tri.vertices;
        _mesh.triangles = tri.indices;
        _mesh.RecalculateNormals();
    }
    public bool PointInNavMesh(Vector3 point)
    {
        Vector3 targetinNavFrame = roomTf.InverseTransformPoint(point);
        Vector2 p = new Vector2(targetinNavFrame.x, targetinNavFrame.y);
        var verts = _mesh.vertices;
        var tris = _mesh.triangles;

        for (int i = 0; i < tris.Length; i+=3)
        {
            Vector2 p0 = verts[tris[i]];
            Vector2 p1 = verts[tris[i+1]];
            Vector2 p2 = verts[tris[i+2]];

            // Barycentric
            Vector2 v0 = p2 - p0, v1 = p1 - p0, v2 = p - p0;
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float invDen = 1f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDen;
            float v = (dot00 * dot12 - dot01 * dot02) * invDen;

            if (u >= 0 && v >= 0 && u + v <= 1)
                return true;
        }
        return false;
    }

    public void ToggleNavMesh()
    {
        floormr.enabled = !floormr.enabled;
    }
    public void ShowNavMesh()
    {
        floormr.enabled = true;
    }
    public void HideNavMesh()
    {
        floormr.enabled = false;
    }
    public void SetColor(Color color)
    {
        floormr.material.color = color;
    }
    public bool PointInHeightLimit(Vector3 point)
    {
        return unreachableHeight.bounds.Contains(point);
    }
    public bool PointOccluded(XObject xobject)
    {
        bool occluded = false;
        Collider[] allColliders = FindObjectsOfType<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col.bounds.Contains(xobject.Position)) 
            {
                if (col.gameObject.layer == 6) continue;
                if (col.transform.name == "Lasso") continue;
                if (col.transform.parent != null && col.transform.parent.name == xobject.transform.name) continue;
                if (col.transform.GetComponentInParent<XObject>() != null && col.transform.GetComponentInParent<XObject>().Name == xobject.Name) continue;
                //if (col.transform.parent.name != null) Debug.Log("col.transform.parent.name" + col.transform.parent.name);
                occluded = true;
            }
        }
        return occluded;
    }
}
