using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualRobotUtil : MonoBehaviour
{
    private bool showVirtualRobots = true;
    private bool showVirtualRobotsCoords = false;
    private string[] debugRobotsNames = { "stretch-decimated-calibrate", "stretch-decimated-preview", "stretch-decimated-target" };
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void ShowHideDebugRobots()
    {
        foreach (string debugRobot in debugRobotsNames)
        {
            GameObject virtualRobot = transform.Find(debugRobot).gameObject;
            if (showVirtualRobots) virtualRobot.SetActive(false);
            else virtualRobot.SetActive(true);
        }
        showVirtualRobots = !showVirtualRobots;
    }
    public void ShowHideDebugCoordinates()
    {
        foreach (string debugRobot in debugRobotsNames)
        {
            GameObject virtualRobot = transform.Find(debugRobot).gameObject;
            DebugRobotCoords coordPanel = virtualRobot.GetComponentInChildren<DebugRobotCoords>();
            if (showVirtualRobotsCoords) coordPanel.ActivateCoordPanel(false);
            else coordPanel.ActivateCoordPanel(true);
        }
        showVirtualRobotsCoords = !showVirtualRobotsCoords;
    }
}
