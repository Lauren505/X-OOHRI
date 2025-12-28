using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InstructionCanvas : MonoBehaviour
{
    public float distanceToCamera = -0.1f;
    public float distanceAboveHand = 0.05f;
    public Transform leftHandTransform;
    public TMP_Text bodyText;
    public GameObject canvas;
    //public HintCanvas hintCanvas;

    void Update()
    {
        transform.position = leftHandTransform.position; //  - Camera.main.transform.rotation * Vector3.forward * distanceToCamera + Vector3.up * distanceAboveHand
        
        //if (hintCanvas.firstTutorialDone && !hintCanvas.secondTutorialDone && !hintCanvas.transform.GetChild(0).gameObject.activeSelf)
        //{
        //    canvas.SetActive(true);
        //    UpdateText("Move the red book to the lower shelf <br>Task:    Move the white bottle under the soda machine and fill it");
        //}
        //else if (hintCanvas.secondTutorialDone && !hintCanvas.studyDone && !hintCanvas.transform.GetChild(0).gameObject.activeSelf)
        //{
        //    canvas.SetActive(true);
        //    UpdateText("Vacuum the back aisle");
        //}
        //else
        //{
        //    canvas.SetActive(false);
        //}
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Vector3.up);
    }

    private void UpdateText(string input)
    {
        bodyText.text = "Task:    " + input;
    }
}
