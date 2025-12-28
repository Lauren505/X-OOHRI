using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TwinConnector : MonoBehaviour
{
    [Header("Lines")]
    private LineRenderer twinConnectorLine;

    [Header("Ghost")]
    public Transform ghostPos;

    [Header("helper")]
    private XObject xobject;
    private ActionSelection action;

    void Start()
    {
        // Establish Children
        xobject = GetComponent<XObject>();
        action = GameObject.Find("ActionSelection").GetComponent<ActionSelection>();
        // Initialize Lines
        twinConnectorLine = transform.Find("twinConnector").GetComponent<LineRenderer>();
        DrawTwinLine(twinConnectorLine, xobject.OriginalPos, xobject.Position);
        twinConnectorLine.useWorldSpace = true;
    }

    void Update()
    {
        if (action.currentAction == "Auto" || !xobject.ActionSpecified)
        {
            twinConnectorLine.enabled = false;
        }
        else
        {
            twinConnectorLine.enabled = true;
        }
        DrawTwinLine(twinConnectorLine, xobject.OriginalPos, xobject.Position);
    }

    private void DrawTwinLine(LineRenderer line, Vector3 startPos, Vector3 endPos)
    {
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

}

