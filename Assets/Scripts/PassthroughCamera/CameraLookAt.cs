using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
    public float distanceToCamera = 80.0f;

    void Update()
    {
        transform.position = Camera.main.transform.position + Camera.main.transform.rotation * Vector3.forward * distanceToCamera;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Vector3.up);
    }
}
