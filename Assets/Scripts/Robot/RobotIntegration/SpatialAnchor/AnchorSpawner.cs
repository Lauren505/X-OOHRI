using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.Util;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using UnityEngine.SceneManagement;

public class AnchorSpawner : MonoBehaviour
{
    [Header("Room Anchors")]
    public GameObject roomObject;
    public MRUKAnchor.SceneLabels centerAnchor;
    private bool anchorsCreated = false;
    private MRUKRoom room;
    private GameObject roomMesh;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector3 spawnScale;
    private bool roomInAdjustment = false;
    private Vector3 initialAnchorPos;
    private Quaternion initialAnchorRot;

    [Header("Robot")]
    public GameObject RobotStretch;

    [Header("Helper")]
    public ControlManager controlManager;

    void Update()
    {
        if (room == null && MRUK.Instance.GetCurrentRoom() != null)
        {
             room = MRUK.Instance.GetCurrentRoom();
            foreach (var anchor in room.Anchors)
            {
                if (anchor.Label == centerAnchor)
                {
                    spawnPosition = anchor.transform.position;
                    spawnScale = anchor.VolumeBounds.Value.size;
                    spawnRotation = anchor.transform.rotation;
                }
            }

            if (!anchorsCreated)
            {
                SpawnRelativeRoom(roomObject);
                roomMesh = roomObject.transform.GetChild(0).gameObject;
                PlaceRelativeRobot(RobotStretch, new Vector3(-0.8f, -1.0f, -2.0f));
                controlManager.UnsetKinematicForAllXObjects();
                anchorsCreated = true;
            }
        }
    }

    private void SpawnRelativeRoom(GameObject roomObject)
    {
        roomObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        controlManager.UpdateOriginalPoseForAll();
    }

    private void PlaceRelativeRobot(GameObject robot, Vector3 offsetPos)
    {
        Transform floorPos = GameObject.Find("Floor").transform;
        robot.transform.position = floorPos.position + Vector3.up * 0.2f;
        Debug.Log("Spawn position: " + spawnPosition);
    }

    public void AdjustRoom()
    {
        GameObject grabInteractor = roomMesh.GetComponentInChildren<Oculus.Interaction.Grabbable>(true).gameObject;
        if (!roomInAdjustment)
        {
            foreach (MeshRenderer ren in roomMesh.GetComponentsInChildren<MeshRenderer>())
            {
                ren.enabled = true;
            }
            controlManager.ShowAllXObjects();
            controlManager.SetKinematicForAllXObjects();
            grabInteractor.SetActive(true);
            initialAnchorPos = grabInteractor.transform.parent.localPosition;
            initialAnchorRot = grabInteractor.transform.parent.localRotation;
        }
        else
        {
            foreach (MeshRenderer ren in roomMesh.GetComponentsInChildren<MeshRenderer>())
            {
                ren.enabled = false;
            }
            controlManager.HideAllExceptActiveXObjects();
            controlManager.UnsetKinematicForAllXObjects();
            grabInteractor.SetActive(false);
        }
        roomInAdjustment = !roomInAdjustment;
    }
    public void SaveRoomOffset()
    {
        Transform child = roomMesh.GetComponentInChildren<Oculus.Interaction.Grabbable>(true).transform.parent;
        
        Quaternion newRot = child.rotation * Quaternion.Inverse(roomMesh.transform.localRotation * initialAnchorRot);
        Vector3 newPos = child.position - newRot * roomMesh.transform.localPosition - newRot * (roomMesh.transform.localRotation * initialAnchorPos);
        roomObject.transform.SetPositionAndRotation(newPos, newRot);
        controlManager.UnsetKinematicForAllXObjects();
        controlManager.UpdateOriginalPoseForAll();
        child.localPosition = initialAnchorPos;
        child.localRotation = initialAnchorRot;
    }
    public void RoomPlusX()
    {
        if (roomInAdjustment)
        {
            roomMesh.transform.localPosition += new Vector3(0.01f, 0f, 0f);
        }
    }
    public void RoomMinusX()
    {
        if (roomInAdjustment)
        {
            roomMesh.transform.localPosition -= new Vector3(0.01f, 0f, 0f);
        }
    }
    public void RoomPlusY()
    {
        if (roomInAdjustment)
        {
            roomMesh.transform.localPosition += new Vector3(0f, 0.01f, 0f);
        }
    }
    public void RoomMinusY()
    {
        if (roomInAdjustment)
        {
            roomMesh.transform.localPosition -= new Vector3(0f, 0.01f, 0f);
        }
    }
    public void RoomPlusZ()
    {
        if (roomInAdjustment)
        {
            roomMesh.transform.localPosition += new Vector3(0f, 0f, 0.01f);
        }
    }
    public void RoomMinusZ()
    {
        if (roomInAdjustment)
        {
            roomMesh.transform.localPosition -= new Vector3(0f, 0f, 0.01f);
        }
    }
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
