using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ActionSelection : MonoBehaviour
{
    [Header("Controller")]
    private Vector2 thumbstickValue;
    private float lastSelectedAngle = -1f;
    private float eps = 0.05f;
    private Quaternion targetRotation;
    private bool releaseFlag = true;

    //office only
    private bool blinkStarted = false;
    private float timerStart;
    
    [Header("Radial Menu")]
    public GameObject radialPartPrefab;
    public GameObject outerRadialPartPrefab;
    public GameObject sendCommandsCanvas;
    private GameObject sendCommandsButton;
    public Transform radialPartCanvas;
    public Transform selectionPartCanvas;
    public float angleBetweenParts = 10;
    public Transform handTransform;
    private int numberOfRadialPart = 0;

    [Header("Current Action")]
    public ControlManager controlManager;
    public ResolutionHandler resolutionHandler;
    public Transform currentActionCanvas;
    public TMP_Text currentActionText;
    public string currentAction = null;
    public string currentState = "Select";
    private SnapFeature snapFeature;

    private List<GameObject> spawnedParts = new List<GameObject>();
    private List<GameObject> spawnedOuterParts = new List<GameObject>();
    public List<bool> activatedParts = new List<bool> { true, true, true };
    private int currentSelectedRadialPart = -1;
    private int currentSelectedOuterRadial = -1;

    private TMP_Text radialText;
    private List<string> actionList = new List<string>{"Move", "Dust", "Wipe"}; // Initial placeholder
    private List<string> outerActionList = new List<string>{"Custom", "Group Move"}; // Initial placeholder

    void Start()
    {
        SpawnRadialPart(true);
        snapFeature = GameObject.FindObjectOfType<SnapFeature>();
    }

    void Update()
    {
        thumbstickValue = OVRInput.Get(controlManager.GetAxis2D(InputFeature.OpenRadialMenu));
        lastSelectedAngle = GetThumbstickAngle();
        UpdateCurrentState();
        ShowResolution();
        ToggleSendCommandsButton();

        // Setup action list
        if (releaseFlag && currentState != "Resolution")
        {
            
            if (controlManager.currentActionList.Count > 3)
            {
                actionList = controlManager.currentActionList.GetRange(0, 3);
            }
            else
            {
                actionList = controlManager.currentActionList;
            }
        }
        
        numberOfRadialPart = actionList.Count;
        
        
        if (actionList.Count > 0)
        {
            if (thumbstickValue.magnitude > 0.2)
            {
                releaseFlag = false;
                if (OVRInput.Get(controlManager.GetRawButton(InputFeature.OpenExtendedMenu)))
                {
                    SpawnRadialPart(false);
                    SpawnOuterRadial();
                    if (lastSelectedAngle >= 0)
                    {
                        GetSelectedOuterRadial(lastSelectedAngle);
                    }
                }
                else
                {
                    SpawnRadialPart(true);
                    KillOuterRadial();
                    currentSelectedOuterRadial = -1;
                    if (lastSelectedAngle >= 0)
                    {
                        GetSelectedRadialPart(lastSelectedAngle);
                    }
                }
            }
            
            if (thumbstickValue.magnitude < eps)
            {
                HideAndTriggerSelected();
                if (!releaseFlag)
                {
                    if (currentState == "Select") 
                    {
                        controlManager.ClearSelectedTarget();
                    }
                    releaseFlag = !releaseFlag;
                }
            }
        }
    }

    public void UpdateCurrentState()
    {
        if (currentState == "Resolution")
        {
            if (currentAction == "Ignore")
            {
                UpdateCurrentText("Select");
            }
            else if (currentAction == "Alternative")
            {
                UpdateCurrentText(currentAction);
                if (releaseFlag)
                {
                    controlManager.HideAllExceptActiveXObjects();
                    XObject alternative = resolutionHandler.GetAlternative();
                    controlManager.ClearSelectedTarget();
                    controlManager.SetSelectedTarget(alternative);
                    ForceSelectionState(true);
                }
            }
        }
        else if (outerActionList.Count > 0 && currentSelectedOuterRadial >= 0)
        {
            currentState = "Action";
            currentAction = outerActionList[currentSelectedOuterRadial];
            UpdateCurrentText(currentAction);
        }
        else if (actionList.Count > 0 && currentSelectedRadialPart >= 0)
        {
            currentState = "Action";
            currentAction = actionList[currentSelectedRadialPart];
            UpdateCurrentText(currentAction);
        }
        else
        {
            currentState = "Select";
            currentAction = null;
            currentSelectedRadialPart = -1;
            currentSelectedOuterRadial = -1;
            UpdateCurrentText("Select");
        }
    }

    public void ForceSelectionState(bool pressed)
    {
        currentState = "Select";
        currentAction = null;
        currentSelectedRadialPart = -1;
        currentSelectedOuterRadial = -1;
        UpdateCurrentText("Select");
    }

    public void HideAndTriggerSelected()
    {
        radialPartCanvas.gameObject.SetActive(false);
        selectionPartCanvas.gameObject.SetActive(false);
    }

    private void GetSelectedRadialPart(float angle)
    {
        float angleStartingLeft = (angle + 90) % 360;
        int selectedIndex = (int) angleStartingLeft * numberOfRadialPart / 180;
        if (selectedIndex < spawnedParts.Count) currentSelectedRadialPart = selectedIndex;
        else
        {
            ForceSelectionState(true);
        }

        selectionPartCanvas.gameObject.SetActive(true);
        if (currentSelectedRadialPart == -1)
        {
            selectionPartCanvas.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
            selectionPartCanvas.GetComponentInChildren<TMP_Text>().color = Color.white;
            selectionPartCanvas.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
        }
        else
        {
            selectionPartCanvas.GetComponent<Image>().color = new Color32(255, 255, 255, 125);
            selectionPartCanvas.GetComponentInChildren<TMP_Text>().color = Color.black;
            selectionPartCanvas.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }
        for (int i = 0; i < spawnedParts.Count; i++)
        {
            if (activatedParts[i] == false)
            {
                spawnedParts[i].GetComponent<Image>().color = new Color32(30, 30, 30, 125);
                spawnedParts[i].GetComponentInChildren<TMP_Text>().color = Color.black;
                spawnedParts[i].transform.localScale = Vector3.one;
            }
            else if (i == currentSelectedRadialPart)
            {
                spawnedParts[i].GetComponent<Image>().color = new Color32(255, 255, 255, 255);
                spawnedParts[i].GetComponentInChildren<TMP_Text>().color = Color.white;
                spawnedParts[i].transform.localScale *= 1.1f;
            }
            else
            {
                spawnedParts[i].GetComponent<Image>().color = new Color32(255, 255, 255, 125);
                spawnedParts[i].GetComponentInChildren<TMP_Text>().color = Color.black;
                spawnedParts[i].transform.localScale = Vector3.one;
            }
        }
    }

    private void GetSelectedOuterRadial(float angle)
    {
        float angleStartingLeft = (angle + 90) % 360;
        int selectedIndex = (int) angleStartingLeft * numberOfRadialPart / 180;
        if (selectedIndex < spawnedOuterParts.Count) currentSelectedOuterRadial = selectedIndex;
        else currentSelectedOuterRadial = -1;

        for (int i = 0; i < spawnedOuterParts.Count; i++)
        {
            if (i == currentSelectedOuterRadial)
            {
                spawnedOuterParts[i].GetComponent<Image>().color = new Color32(255, 255, 255, 255);
                spawnedOuterParts[i].GetComponentInChildren<TMP_Text>().color = Color.white;
                spawnedOuterParts[i].transform.localScale *= 1.1f;
            }
            else
            {
                spawnedOuterParts[i].GetComponent<Image>().color = new Color32(255, 255, 255, 125);
                spawnedOuterParts[i].GetComponentInChildren<TMP_Text>().color = Color.black;
                spawnedOuterParts[i].transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            }
        }
    }

    private void SpawnRadialPart(bool showtext)
    {
        radialPartCanvas.gameObject.SetActive(true);
        radialPartCanvas.position = handTransform.position - handTransform.right * 0.01f;
        radialPartCanvas.rotation = targetRotation;
        radialPartCanvas.Rotate(new Vector3(25, 0, 0), Space.Self);
        
        foreach (var item in spawnedParts)
        {
            Destroy(item);
        }

        spawnedParts.Clear();
        
        for (int i = 0; i < numberOfRadialPart; i++)
        {
            float angle = -i * 180 / numberOfRadialPart - angleBetweenParts / 2 + 90;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

            GameObject spawnedRadialPart = Instantiate(radialPartPrefab, radialPartCanvas);
            spawnedRadialPart.transform.position = radialPartCanvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;

            spawnedRadialPart.GetComponent<Image>().fillAmount = (0.5f / (float)numberOfRadialPart) - (angleBetweenParts / 360);

            spawnedParts.Add(spawnedRadialPart);

            radialText = spawnedRadialPart.GetComponentInChildren<TMP_Text>();
            if (showtext)
            {
                radialText.text = $"{actionList[i]}";
                float textAngle = angle - (90 / numberOfRadialPart - angleBetweenParts / 2);
                Vector3 rotatedTextOffset = Quaternion.AngleAxis(textAngle, radialPartCanvas.forward) * radialPartCanvas.up;
                radialText.transform.position += rotatedTextOffset.normalized * 0.1f;
                radialText.transform.localEulerAngles = new Vector3(0, 0, -angle);
            }
            else
            {
                radialText.text = "";
            }
        }
    }

    private float GetThumbstickAngle()
    {
        float thumbstickAngle = Mathf.Rad2Deg * Mathf.Atan2(thumbstickValue.x, thumbstickValue.y);
        if (thumbstickValue.magnitude > 0.5)
        {
            return (thumbstickAngle + 360) % 360;
        }
        else return -1f;
    }

    private void UpdateCurrentText(string text)
    {
        targetRotation = Quaternion.LookRotation(handTransform.forward, handTransform.up);

        currentActionCanvas.position = handTransform.position + handTransform.forward * 0.03f + handTransform.up * 0.02f - handTransform.right * 0.01f;
        currentActionCanvas.rotation = targetRotation;
        currentActionCanvas.Rotate(new Vector3(25, 0, 0), Space.Self);

        currentActionText.text = text;
    }

    private void KillOuterRadial()
    {
        foreach (var item in spawnedOuterParts)
        {
            Destroy(item);
        }

        spawnedOuterParts.Clear();
    }
    
    private void SpawnOuterRadial()
    {
        int numberOfOuterRadial = 2;

        KillOuterRadial();

        for (int i = 0; i < numberOfOuterRadial; i++)
        {
            float angle = -i * 180 / numberOfOuterRadial - angleBetweenParts / 2 + 90;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

            GameObject spawnedRadialPart = Instantiate(outerRadialPartPrefab, radialPartCanvas);
            spawnedRadialPart.transform.position = radialPartCanvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;
            spawnedRadialPart.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

            spawnedRadialPart.GetComponent<Image>().fillAmount = (0.5f / (float)numberOfOuterRadial) - (angleBetweenParts / 360);

            spawnedOuterParts.Add(spawnedRadialPart);

            radialText = spawnedRadialPart.GetComponentInChildren<TMP_Text>();
            radialText.text = $"{outerActionList[i]}";
            float textAngle = angle - (90 / numberOfOuterRadial - angleBetweenParts / 2);
            Vector3 rotatedTextOffset = Quaternion.AngleAxis(textAngle, radialPartCanvas.forward) * radialPartCanvas.up;
            radialText.transform.position += rotatedTextOffset.normalized * 0.13f;
            radialText.transform.localEulerAngles = new Vector3(0, 0, -angle);
        }
    }

    private void ShowResolution()
    {
        if (currentAction == "Auto" && activatedParts[0] != false)
        {
            if (snapFeature != null)
            {
                if (thumbstickValue.magnitude > 0.2)
                {
                    snapFeature.AutoSnapAnim();
                }
            }
        }
        else if (currentAction == "Alternative" && activatedParts[1] != false)
        {
            currentState = "Resolution";
            XObject alternative = resolutionHandler.GetAlternative();
            if (alternative != null)
            {
                alternative.InDebug = true;
                alternative.ShowMesh();
            }
        }
        else if (currentAction == "Ignore" && activatedParts[2] != false)
        {
            currentState = "Resolution";
            resolutionHandler.DisableConstraint();
        }
    }

    private void ToggleSendCommandsButton()
    {
        if (sendCommandsCanvas.activeSelf)
        {
            targetRotation = Quaternion.LookRotation(handTransform.forward, handTransform.up);
            sendCommandsCanvas.transform.position = handTransform.position + handTransform.forward * 0.1f + handTransform.up * 0.012f - handTransform.right * 0.01f;
            sendCommandsCanvas.transform.rotation = targetRotation;
            sendCommandsCanvas.transform.Rotate(new Vector3(25, 0, 0), Space.Self);
        }
        if (OVRInput.GetDown(controlManager.GetRawButton(InputFeature.ToggleSendCommandsButton)))
        {
            currentActionCanvas.gameObject.SetActive(sendCommandsCanvas.activeSelf);
            sendCommandsCanvas.SetActive(!sendCommandsCanvas.activeSelf);
        }
    }
    public void FadeOutSendCommandsButton()
    {
        StartCoroutine(FadeOut());
    }
    private IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        sendCommandsCanvas.SetActive(false);
        currentActionCanvas.gameObject.SetActive(true);
        sendCommandsCanvas.gameObject.GetComponentInChildren<Toggle>(true).isOn = false;
    }
}
