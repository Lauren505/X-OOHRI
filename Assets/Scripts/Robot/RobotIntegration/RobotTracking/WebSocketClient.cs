using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System;

public delegate void MessageReceived(long ts, string str);
public delegate void ConnectionOpened();
public delegate void ConnectionClosedOrFailed();

public class WebSocketClient : MonoBehaviour
{
    WebSocket websocket;
    int numFailedLookups;
    long lastConnectionTrialTs;

    public bool open = false;
    public string ip;
    public int port;

    public MessageReceived messageReceived;

    public ConnectionOpened connectionOpened;
    public ConnectionClosedOrFailed connectionClosedOrFailed;

    public void Start()
    {
        connectionOpened += () => { };
        connectionClosedOrFailed += () => { };
    }

    async public void ConnectToServer()
    {
        websocket = new WebSocket($"ws://{ip}:{port}");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            connectionOpened();
            open = true;
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
            open = false;
            connectionClosedOrFailed();
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed or failed!");
            open = false;
            connectionClosedOrFailed();
        };

        websocket.OnMessage += (bytes) =>
        {
            //Debug.Log("OnMessage!");
            //Debug.Log(bytes);

            // getting the message as a string
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log("Received " + message);
            ProcessMessage(message);

        };

        await websocket.Connect();

    }

    public void ProcessMessage(string message)
    {
        var parts = message.Split(",", 3);
        var ts = long.Parse(parts[0]);
        var messageType = parts[1];

        long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        long diff = ms - ts;
        Debug.Log($"android: {ts} | unity: {ms} | diff: {diff}");


        var attributes = parts[2];
        if (messageReceived != null)
        {
            messageReceived(ts, attributes);
        }
    }


    public void Update()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            // send alive
        }
        else if (websocket != null && websocket.State == WebSocketState.Connecting)
        {

        }
        else if (websocket != null && websocket.State == WebSocketState.Closing)
        {

        }
        else if (websocket == null || websocket.State == WebSocketState.Closed)
        {
            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastConnectionTrialTs > 10000)
            {
                ConnectToServer();
                numFailedLookups = 0;
                lastConnectionTrialTs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
            {
                numFailedLookups++;
            }
        }

        if (websocket != null) websocket.DispatchMessageQueue();
    }

    async public void SendWebSocketMessage(String message)
    {
        Debug.Log($"SendWebSocketMessage: client is sending: {message}");
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.Log($"client is sending text: {message}");
            await websocket.SendText(message);
        }
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}
