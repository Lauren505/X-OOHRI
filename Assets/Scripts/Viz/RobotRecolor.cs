using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RobotRecolor : MonoBehaviour
{
    public Material robotMat;
    public Color robotCalibColor;
    public Color robotPrevColor;
    public Color robotTarColor;
    private float colorIntensity = 3.0f;

    void Start()
    {
        UpdateColor("stretch-decimated-calibrate", robotCalibColor * colorIntensity);
        UpdateColor("stretch-decimated-preview", robotPrevColor * colorIntensity);
        UpdateColor("stretch-decimated-target", robotTarColor * colorIntensity);
    }


    private void UpdateColor(string robot, Color color)
    {
        GameObject robotGO = transform.Find(robot).gameObject;
        if (robotGO != null)
        {
            foreach (MeshRenderer ren in robotGO.GetComponentsInChildren<MeshRenderer>())
            {
                ren.material = robotMat;
                ren.material.SetColor("_Fresnel_Color", color);
            }
        }
    }
}
