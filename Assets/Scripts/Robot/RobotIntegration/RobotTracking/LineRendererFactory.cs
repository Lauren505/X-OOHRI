using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LineRendererFactory 
{
    public static LineRenderer makeLineRenderer(GameObject parent, string name, Color color)
    {
        var lineGameObject = new GameObject(name);
        lineGameObject.transform.SetPositionAndRotation(parent.transform.position, parent.transform.rotation);
        lineGameObject.transform.parent = parent.transform;
        
        var lineRenderer = lineGameObject.AddComponent<LineRenderer>();
        var matBlue = new Material(Shader.Find("Standard"));
        matBlue.SetColor("_Color", color);
        lineRenderer.material = matBlue;
        lineRenderer.widthMultiplier = 0.03f;

        return lineRenderer;
    }
}
