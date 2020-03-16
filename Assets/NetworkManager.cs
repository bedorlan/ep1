using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

enum Codes
{
    noop = 0, // [0]
    start = 1, // [1, playerNumber: int]
    newPlayerDestination = 2, // [2, positionX: float]
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager singleton;

    public GameObject playerPrefab;
    public new GameObject camera;

    private Telepathy.Client client;
    private Dictionary<Codes, Action<List<String>>> codesMap;
    private int playerNumber;

    private GameObject localPlayer;
    private GameObject remotePlayer;

    void Awake()
    {
        singleton = this;

        // update even if window isn't focused, otherwise we don't receive.
        Application.runInBackground = true;

        // use Debug.Log functions for Telepathy so we can see it in the console
        Telepathy.Logger.Log = Debug.Log;
        Telepathy.Logger.LogWarning = Debug.LogWarning;
        Telepathy.Logger.LogError = Debug.LogError;

        codesMap = new Dictionary<Codes, Action<List<String>>> {
            { Codes.start, StartGame },
            { Codes.newPlayerDestination, OnRemoteNewDestination }
        };
    }

    void Start()
    {
        client = new Telepathy.Client();
        client.Connect("localhost", 7777);
    }

    void Update()
    {
        Telepathy.Message msg;
        while (client.GetNextMessage(out msg))
        {
            switch (msg.eventType)
            {
                case Telepathy.EventType.Connected:
                    Debug.Log("Connected");
                    break;
                case Telepathy.EventType.Data:
                    ProcessRemoteMsg(msg.data);
                    break;
                case Telepathy.EventType.Disconnected:
                    Debug.Log("Disconnected");
                    break;
            }
        }
    }

    class SupportedJsonType
    {
        public List<string> items;
    }


    private void ProcessRemoteMsg(byte[] data)
    {
        var asciiData = Encoding.ASCII.GetString(data);
        Debug.Log("NewMessage=" + asciiData);

        var supportedJson = string.Format("{{\"items\":{0}}}", asciiData);
        var jsonData = JsonUtility.FromJson<SupportedJsonType>(supportedJson).items;
        Codes code = (Codes)int.Parse(jsonData[0]);
        if (!codesMap.ContainsKey(code))
        {
            Debug.LogError("unknown message=" + asciiData);
        }
        codesMap[code].Invoke(jsonData);
    }

    private void StartGame(List<string> data)
    {
        playerNumber = int.Parse(data[1]);
        Debug.Log("myPlayer=" + playerNumber);

        localPlayer = Instantiate(playerPrefab);
        camera.GetComponent<MainCamera>().objectToFollow = localPlayer;
        localPlayer.GetComponent<PlayerBehaviour>().Initialize(playerNumber, true);

        remotePlayer = Instantiate(playerPrefab);
        remotePlayer.GetComponent<PlayerBehaviour>().Initialize(playerNumber == 0 ? 1 : 0, false);

        Physics2D.IgnoreCollision(
            localPlayer.GetComponent<Collider2D>(),
            remotePlayer.GetComponent<Collider2D>());
    }

    public void NewLocalPlayerDestination(float newDestinationX)
    {
        var msg = string.Format("[{0}, {1}]", (int)Codes.newPlayerDestination, newDestinationX);
        client.Send(Encoding.ASCII.GetBytes(msg));
    }

    private void OnRemoteNewDestination(List<string> data)
    {
        var newDestination = float.Parse(data[1]);
        remotePlayer.GetComponent<PlayerBehaviour>().NewDestination(newDestination);
    }

    void OnApplicationQuit()
    {
        // the client/server threads won't receive the OnQuit info if we are
        // running them in the Editor. they would only quit when we press Play
        // again later. this is fine, but let's shut them down here for consistency
        client.Disconnect();
    }
}
