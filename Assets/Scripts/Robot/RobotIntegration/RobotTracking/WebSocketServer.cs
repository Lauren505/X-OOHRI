using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Oculus.Interaction;

public delegate void MessageReceivedAtServer(long ts, string str);

public class WebSocketServer : MonoBehaviour
{
    private Thread thread;
    public int port;
    public bool tcpOnly;
    public MessageReceivedAtServer messageReceivedAtServer;
    public long lastMessageTimestamp = 0;
    public ConcurrentQueue<string> messages = new ConcurrentQueue<string>();

    [HideInInspector]
    public string startWithFilter = "";

    public NetworkReachability networkReachability;

    private bool abortRequested = false;
    private TcpClient currentClient;
    private NetworkStream currentStream;

    public void Start()
    {
        networkReachability = Application.internetReachability;

        thread = new Thread(new ThreadStart(Listen));
        thread.IsBackground = true;
        thread.Start();
    }

    public void Update()
    {
        
    }

    public void Log(string s)
    {
        Debug.Log($"App:Materializer.WebSocketServer: {s}");
    }


    public void LogWarning(string s)
    {
        Debug.LogWarning($"App:Materializer.WebSocketServer: {s}");
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    /// <summary>
    /// https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server
    /// </summary>
    public void Listen()
    {
        string ip = GetLocalIPAddress();
        var server = new TcpListener(IPAddress.Parse(ip), port);

        server.Start();

        while (true)
        {
            Log($"Server has started on {ip}:{port}, Waiting for a connection...");

            Task<TcpClient> firstClientTask = server.AcceptTcpClientAsync();
            currentClient = firstClientTask.Result;
            currentStream = currentClient.GetStream();
            Log($"A client connected {currentClient}.");

            Task<TcpClient> nextClientTask = server.AcceptTcpClientAsync();
            Log("Waiting for second client asynchronously.");

            // enter to an infinite cycle to be able to handle every change in stream
            while (true)
            {
                while (true)
                {
                    if (abortRequested)
                    {
                        Cleanup();
                        return;
                    }
                    if (nextClientTask.IsCompleted)
                    {
                        Log($"Switching to new client!");
                        Cleanup();
                        currentClient = nextClientTask.Result;
                        currentStream = currentClient.GetStream();
                        nextClientTask = server.AcceptTcpClientAsync();
                    }
                    if (!currentStream.DataAvailable)
                    {
                        continue;
                    }
                    if (currentClient.Available < 3) // match against "get"
                    {
                        continue;
                    }

                    break;
                };

                var numBytesToRead = currentClient.Available;
                byte[] bytes = new byte[numBytesToRead];
                currentStream.Read(bytes, 0, numBytesToRead);
                string s = Encoding.UTF8.GetString(bytes);

                Debug.Log("data available and read");

                if (tcpOnly)
                {
                    Log(s);
                    if (s.StartsWith(startWithFilter))
                    {
                        messages.Enqueue(s);
                        lastMessageTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    }
                }
                else
                {
                    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                    {
                        Log($"=====Handshaking from client=====\n{s}");

                        // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                        // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                        // 3. Compute SHA-1 and Base64 hash of the new value
                        // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                        string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                        string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                        byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                        string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                        byte[] response = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 101 Switching Protocols\r\n" +
                            "Connection: Upgrade\r\n" +
                            "Upgrade: websocket\r\n" +
                            "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                        currentStream.Write(response, 0, response.Length);
                    }
                    else
                    {
                        bool fin = (bytes[0] & 0b10000000) != 0,
                            mask = (bytes[1] & 0b10000000) != 0;  // must be true, "All messages from the client to the server have this bit set"
                        int opcode = bytes[0] & 0b00001111; // expecting 1 - text message
                        ulong offset = 2;
                        ulong msglen = (ulong)(bytes[1] & 0b01111111);

                        if (msglen == 126)
                        {
                            msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                            offset = 4;
                        }
                        else if (msglen == 127)
                        {
                            msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                            offset = 10;
                        }

                        if (msglen == 0)
                        {
                            Log("msglen == 0");
                        }
                        else if (mask)
                        {
                            try
                            {
                                byte[] decoded = new byte[msglen];
                                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                                offset += 4;

                                for (ulong i = 0; i < msglen; ++i)
                                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                                string text = Encoding.UTF8.GetString(decoded);
                                Log(text);
                                if (messageReceivedAtServer != null)
                                {
                                    messageReceivedAtServer(DateTimeOffset.Now.ToUnixTimeMilliseconds(), text);
                                }
                            }
                            catch
                            {
                                LogWarning("error while decoding!");
                            }
                        }
                        else
                            Log("mask bit not set");
                    }
                }
            }
        }
    }

    private void Cleanup()
    {
        Log("Cleaning up!");
        if (currentStream != null)
        {
            currentStream.Close();
        }
        if (currentClient != null)
        {
            currentClient.Close();
            currentClient.Dispose();
        }
    }

    public void SendWebSocketMessage(string message)
    {
        Debug.Log($"currentStream: {currentStream}, currentClient: {currentClient}");
        if (currentClient != null && currentClient.Connected)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] frame = new byte[data.Length + 2];
            frame[0] = 0x81; // Final fragment, text frame
            frame[1] = (byte)data.Length; // Length of the payload
            Array.Copy(data, 0, frame, 2, data.Length);
            currentStream.Write(frame, 0, frame.Length);
        }
        else
        {
            LogWarning($"Cannot send message: No client connected ({currentClient == null}).");
        }
    }

    public void OnDestroy()
    {
        Log("OnDestroy");
        abortRequested = true;
    }
}
