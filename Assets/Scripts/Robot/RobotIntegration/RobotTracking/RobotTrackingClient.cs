using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotTrackingClient : MonoBehaviour
{
    public bool sendMsg;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(sendMsg){
            var webSocketClient = GetComponent<WebSocketClient>();
            webSocketClient.SendWebSocketMessage("hi there");
            Debug.Log("sent!");
            sendMsg = false;
        }
    }


}
