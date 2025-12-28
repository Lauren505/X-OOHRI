using UnityEngine;

public class SetRenderQueue : MonoBehaviour
{
    public Material material;
    public int customRenderQueue = 3100;

    void Start()
    {
        if (material != null)
        {
            material.renderQueue = customRenderQueue;
        }
    }
}