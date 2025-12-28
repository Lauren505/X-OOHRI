using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConstraintLabel : MonoBehaviour
{
    public GameObject content;
    public GameObject tooltip;
    public TMP_Text text;
    public TMP_Text tooltipText;
    public float offset = 0.1f;
    public float offsetY = 0.05f;
    public bool hide = false;
    public bool InResolution = false;
    
    void Update()
    {
    }
    void LateUpdate()
    {
        transform.LookAt(transform.position - Camera.main.transform.rotation * Vector3.forward, Vector3.up);
    }

    public void ShowLabel()
    {
        content.SetActive(!hide);
    }
    public void HideLabel()
    {
        content.SetActive(false);
    }
    public void SetLabelText(string input)
    {
        text.text = input;
    }
    public void SetTooltipText(string input)
    {
        tooltipText.text = input;
    }
    public void ToggleTooltip()
    {
        if (!InResolution)
        {
            //tooltip.SetActive(true);
            InResolution = true;
        }
        else
        {
            //tooltip.SetActive(false);
            InResolution = false;
        }
    }

    public void SetLabelPosition(Vector3 targetPos)
    {
        transform.position = targetPos - Camera.main.transform.rotation * Vector3.forward * offset + Vector3.up * offsetY;
    }
}
