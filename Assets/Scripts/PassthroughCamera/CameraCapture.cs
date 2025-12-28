using PassthroughCameraSamples;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CameraCapture : MonoBehaviour
{
    public GameObject cameraViewport;
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private WebCamTextureManager webCamTextureManager;
    [SerializeField] 
    private Text m_debugText;
    [SerializeField] 
    private GPTClient gptClient;

    private Texture2D image = null;

    void Update()
    {
        // image = CaptureView();
        // SaveCapture(image);

        //if (OVRInput.GetDown(OVRInput.Button.One) && cameraViewport.activeSelf)
        //{
        //    image = CaptureWebCam(webCamTextureManager.WebCamTexture);
        //    m_debugText.text = "Webcam captured.";
        //    gptClient.SubmitImage(image);
        //    //sendQueryScript.SendImageQuery("Reason about this image for future queries", image);
        //}
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            SendImageQuery();
        }
    }
    public void ToggleCamera()
    {
        if (cameraViewport.activeSelf)
        {
            cameraViewport.SetActive(false);
        }
        else
        {
            cameraViewport.SetActive(true);
        }
    }
    public void SendImageQuery()
    {
        if (cameraViewport.activeSelf)
        {
            image = CaptureWebCam(webCamTextureManager.WebCamTexture);
            m_debugText.text = "Webcam captured.";
            gptClient.SubmitImage(image);
            //sendQueryScript.SendImageQuery("Reason about this image for future queries", image);
        }
    }

    public Texture2D CaptureWebCam(WebCamTexture webCamTex)
    {
        Texture2D image = new Texture2D(webCamTex.width, webCamTex.height, TextureFormat.RGB24, false);
        image.SetPixels(webCamTex.GetPixels());
        image.Apply();
        Debug.Log("Webcam captured.");
        return image;
    }

    public Texture2D CaptureView()
    {
        int width = Screen.width;
        int height = Screen.height;
        RenderTexture rt = new RenderTexture(width, height, 24);
        mainCamera.targetTexture = rt;

        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;

        mainCamera.Render();
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        mainCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        return image;
    }

    public void SaveCapture(Texture2D tex)
    {
        byte[] bytes = tex.EncodeToPNG();
        string path = Application.persistentDataPath + "/capture.png";
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log("Saved image to: " + path);
    }

    private IEnumerator Start()
    {
        while (webCamTextureManager.WebCamTexture == null)
        {
            yield return null;
        }
        Debug.Log("WebCamTexture object currently ready and playing");
    }
}
