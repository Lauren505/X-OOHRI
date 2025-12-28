using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugRobotCoords : MonoBehaviour
{
    public TMP_Text bodyText;
    public GameObject coordPanel;

    // Update is called once per frame
    void Update()
    {
        string txt = "Translation: " + transform.parent.position + "<br>Rotation: " + transform.parent.rotation.eulerAngles;
        UpdateText(txt);
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Vector3.up);
    }
    public void AppendAMCLDebugPose(Vector3 amclPos, Quaternion amclRot)
    {
        bodyText.text += "<br>AMCL Translation: " + amclPos + "<br>AMCL Rotation: " + amclRot.eulerAngles;
    }

    private void UpdateText(string input)
    {
        bodyText.text = input;
    }

    public void ActivateCoordPanel(bool status)
    {
        coordPanel.SetActive(status);
    }
}
