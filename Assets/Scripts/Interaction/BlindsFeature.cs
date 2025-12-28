using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Oculus.Interaction;

public class BlindsFeature : MonoBehaviour
{
    public ControlManager controlManager;
    public ActionSelection radialMenu;
    public ActionHandler actionHandler;
    public Renderer blindStick;
    public ConstraintLabel stickConstraintLabel;
    public Transform blind;
    public Transform rightHandTrans;
    public OVRInput.RawButton drawButton;

    private XObject blindXobject;
    private Renderer blindCube;
    
    private float startHeight;
    private float endHeight;
    private float minHeight = 0.01f;
    private float maxHeight = 1f;

    void Start()
    {
        blindXobject = transform.GetComponent<XObject>();
        blindCube = blind.GetComponentInChildren<Renderer>();
        blindCube.enabled = false;
        blindStick.enabled = false;
    }

    void Update()
    {
        if (controlManager.selectedXObjects.Count == 0) return;

        if (controlManager.selectedXObjects.First().Name == "Blind")
        {
            blindCube.enabled = true;
            blindStick.enabled = true;
            radialMenu.activatedParts = new List<bool> { false, false, true };
            if (radialMenu.currentAction == "dim" ||  radialMenu.currentAction == "brighten")
            {
                blindStick.materials[0].SetColor("_BaseColor", Color.red);
                stickConstraintLabel.ShowLabel();
                stickConstraintLabel.SetLabelText(ExplanationMapping.Label(ExplanationTag.Stretch));
            }
            else
            {
                blindStick.materials[0].SetColor("_BaseColor", new Color(1, 1, 1, 0.6f));
                stickConstraintLabel.HideLabel();
                actionHandler.UpdatePickPose(blindXobject);
                if (OVRInput.GetDown(drawButton))
                {
                    startHeight = rightHandTrans.position.y;
                }
                if (OVRInput.Get(drawButton))
                {
                    endHeight = rightHandTrans.position.y;
                    ScaleWater(endHeight, startHeight);
                }
            }
        }
        else
        {
            blindCube.enabled = false;
            blindStick.enabled = false;
            radialMenu.activatedParts = new List<bool> { true, true, true };
        }
    }

    private void ScaleWater(float end, float start)
    {
        float diff = Mathf.Max((end - start), 0f);
        float scale = Mathf.Min(diff * 1.5f, 1f);
        blind.localScale = new Vector3(1, 1, maxHeight - (maxHeight - minHeight) * scale);
    }
}
