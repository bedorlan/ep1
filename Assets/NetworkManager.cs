using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SimpleJSON;
using UnityEngine;

enum Codes
{
    noop = 0, // [0]
    start = 1, // [1, playerNumber: int]
    newPlayerDestination = 2, // [2, positionX: float]
    newVoters = 3, // [3, ...voters: [id: number, positionX: number]]
    measureLatency = 4, // [4]
}

public class NetworkManager : MonoBehaviour
{
    const float FLOOR_LEVEL_Y = -4f;

    public static NetworkManager singleton;

    public GameObject playerPrefab;
    public GameObject voterPrefab;
    public new GameObject camera;

    private Telepathy.Client client;
    private Dictionary<Codes, Action<SimpleJSON.JSONNode>> codesMap;
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

        codesMap = new Dictionary<Codes, Action<SimpleJSON.JSONNode>> {
            { Codes.start, StartGame },
            { Codes.newPlayerDestination, OnRemoteNewDestination },
            { Codes.newVoters, OnNewVoters },
            { Codes.measureLatency, OnMeasureLatency }
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


    private void ProcessRemoteMsg(byte[] data)
    {
        var asciiData = Encoding.ASCII.GetString(data);
        var jsonData = SimpleJSON.JSON.Parse(asciiData);
        Codes code = (Codes)jsonData[0].AsInt;
        if (!codesMap.ContainsKey(code))
        {
            Debug.LogError("unknown message=" + asciiData);
        }
        codesMap[code].Invoke(jsonData);
    }

    private void StartGame(SimpleJSON.JSONNode data)
    {
        playerNumber = data[1].AsInt;
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
        SendNetworkMsg(msg);
    }

    private void SendNetworkMsg(string msg)
    {
        client.Send(Encoding.ASCII.GetBytes(msg + "\n"));
    }

    private void OnRemoteNewDestination(SimpleJSON.JSONNode data)
    {
        var newDestination = data[1].AsFloat;
        remotePlayer.GetComponent<PlayerBehaviour>().Remote_NewDestination(newDestination);
    }

    private void OnNewVoters(SimpleJSON.JSONNode data)
    {
        data.Remove(0);
        foreach (var voter in data.Values)
        {
            var voterId = voter[0].AsInt;
            var voterPositionX = voter[1].AsFloat;

            var newVoter = Instantiate(voterPrefab);
            newVoter.GetComponent<VoterBehaviour>().SetId(voterId);
            newVoter.transform.position = new Vector3(voterPositionX, FLOOR_LEVEL_Y + .1f, 0f);
        }
    }

    private void OnMeasureLatency(JSONNode data)
    {
        var msg = string.Format("[{0}]", (int)Codes.measureLatency);
        SendNetworkMsg(msg);
    }

    public void VoterClicked(VoterBehaviour voter)
    {
        localPlayer.GetComponent<PlayerBehaviour>().ChaseVoter(voter);
    }

    void OnApplicationQuit()
    {
        // the client/server threads won't receive the OnQuit info if we are
        // running them in the Editor. they would only quit when we press Play
        // again later. this is fine, but let's shut them down here for consistency
        client.Disconnect();
    }
}
