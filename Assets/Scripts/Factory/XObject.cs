using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class XObject : MonoBehaviour
{
    // Immutable propoerties
    // Use API when connected to a runtime VLM client
    public string Name;
    public float Weight; // grams
    public string Material;
    public bool HasHandle;
    public bool IsReceptacle;
    public List<string> actions = new List<string>();

    // Mutable properties
    public Vector3 Position;
    public Vector3 Size { get ; set; } // cm

    // Intercation properties
    public Pose PickBasePose;
    public Pose PlaceBasePose;
    public Pose PickGripper { get; set; }
    public Pose PlaceGripper { get; set; }
    public int GripperInd { get; set; }
    public bool ActionSpecified;
    public bool IsSelected { get; set; }
    public bool InDebug;
    public Vector3 OriginalPos;
    public Quaternion OriginalRot { get; set; }

    // Internal helper properties
    public Transform ghost { get; set; }
    private List<Transform> grippers = new List<Transform>();
    private List<Transform> endEffectors = new List<Transform>();
    private Renderer[] rens;
    private List<Color> defaultColors = new List<Color>();
    private ConstraintLabel _constraintLabel;
    public ConstraintLabel constraintLabel => _constraintLabel ??= GetComponentInChildren<ConstraintLabel>();

    public void Init()
    {
        ghost = transform.Find("GhostMesh");
        Mesh ghostMesh = ghost.GetComponent<MeshFilter>().mesh;
        Position = ghost.position;
        OriginalPos = Position;
        OriginalRot = ghost.rotation;
        Size = ghostMesh.bounds.size;
        FindChildGrippersEndEffectors();
        rens = GetComponentsInChildren<MeshRenderer>();
        GetDefaultColors();
    }
    void Update()
    {
        Position = ghost.position;
        if (!constraintLabel.InResolution)
        {
            if (IsSelected || ActionSpecified || InDebug) ShowMesh(); // should be selected or action set
            else HideMesh();
        }
    }

    #region "Properties"
    public abstract string GetClassName();
    public void SetOriginalPose(Vector3 position, Quaternion rotation)
    {
        OriginalPos = position;
        OriginalRot = rotation;
    }
    #endregion
    #region "Visibility"
    public void ShowMesh()
    {
        ghost.GetComponent<Renderer>().enabled = true;
        //foreach (MeshRenderer ren in rens)
        //{
        //    ren.enabled = true;
        //}
    }
    public void HideMesh()
    {
        ghost.GetComponent<Renderer>().enabled = false;
        //foreach (MeshRenderer ren in rens)
        //{
        //    ren.enabled = false;
        //}
    }
    public void SetKinematic()
    {
        Rigidbody rb = ghost.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }
    public void UnsetKinematic()
    {
        Rigidbody rb = ghost.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }
    }
    public void EnableMove()
    {
        Grabbable grabbable = ghost.GetComponentInChildren<Grabbable>(true);
        GameObject rayInteracable = ghost.Find("Ray Interaction").gameObject;
        GhostRayInteraction ghostRayInteraction = GetComponent<GhostRayInteraction>();
        if (grabbable != null && rayInteracable != null)
        {
            Debug.Log("grabbable: " + grabbable);
            ghostRayInteraction.ChangePointable(grabbable);
            rayInteracable.SetActive(false);
        }
    }
    public void DisableMove()
    {
        Grabbable grabbable = ghost.GetComponentInChildren<Grabbable>(true);
        RayInteractable rayInteracable = ghost.Find("Ray Interaction").GetComponent<RayInteractable>();
        GhostRayInteraction ghostRayInteraction = GetComponent<GhostRayInteraction>();
        if (rayInteracable != null) ghostRayInteraction.ChangePointable(rayInteracable);
    }
    #endregion
    #region "EndEffector"
    public List<Transform> GetGripperTf()
    {
        return grippers;
    }

    public List<Transform> GetEndEffectorTf()
    {
        return endEffectors;
    }
    private void FindChildGrippersEndEffectors()
    {
        Transform ghostMesh = transform.Find("GhostMesh");
        foreach (Transform child in ghostMesh)
        {
            if (child.name == "Gripper")
            {
                grippers.Add(child);
                endEffectors.Add(child.GetChild(0).Find("link_SG3_gripper_body"));
            }
        }
    }
    #endregion
    #region "Color Code"
    public void SetColor(Color targetColor = default)
    {
        int count = 0;
        foreach (MeshRenderer ren in rens)
        {
            for (int i = 0; i < ren.materials.Length; i++)
            {
                if (targetColor == default)
                {
                    ren.materials[i].SetColor("_BaseColor", defaultColors[count]);
                }
                else
                {
                    ren.materials[i].SetColor("_BaseColor", targetColor);
                }

                count++;
            }
        }
    }

    public void SetAlpha(float targetAlpha)
    {
        foreach (MeshRenderer ren in rens)
        {
            Color[] currentColors = new Color[ren.materials.Length];
            for (int i = 0; i < ren.materials.Length; i++)
            {
                currentColors[i] = ren.materials[i].GetColor("_BaseColor");
                currentColors[i].a = targetAlpha;
                ren.materials[i].SetColor("_BaseColor", currentColors[i]);
            }
        }
    }

    private void GetDefaultColors()
    {
        foreach (MeshRenderer ren in rens)
        {
            // defaultColors = new Color[ren.materials.Length];
            for (int i = 0; i < ren.materials.Length; i++)
            {
                if (ren.materials[i].HasProperty("_BaseColor"))
                {
                    defaultColors.Add(ren.materials[i].GetColor("_BaseColor"));
                }
            }
        }
    }
    #endregion
}
