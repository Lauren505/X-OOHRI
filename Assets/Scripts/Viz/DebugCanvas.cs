using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugCanvas : MonoBehaviour
{
    public Transform leftHandTransform;

    void Update()
    {
        transform.position = leftHandTransform.position;
    }
    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Vector3.up);
    }
    public void ToggleMenu()
    {
        GameObject menu = transform.GetChild(0).gameObject;
        if (menu.activeSelf)
        {
            menu.SetActive(false);
        }
        else
        {
            menu.SetActive(true);
        }
    }

}
