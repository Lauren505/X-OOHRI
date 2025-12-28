using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotTrackingServer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // var webSocketServer = GetComponent<WebSocketServer>; 
        // webSocketServer.messageReceivedAtServer += MessageReceivedAtServer;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MessageReceivedAtServer(long ts, string str){
        Debug.Log($"{ts}, {str}");
    }
}
