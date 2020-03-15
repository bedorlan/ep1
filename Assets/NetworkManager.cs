using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private Telepathy.Client client;

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
                    client.Send(Encoding.ASCII.GetBytes("12"));
                    client.Send(Encoding.ASCII.GetBytes("123"));
                    break;
                case Telepathy.EventType.Data:
                    Debug.Log("Data: " + Encoding.ASCII.GetString(msg.data));
                    break;
                case Telepathy.EventType.Disconnected:
                    Debug.Log("Disconnected");
                    break;
            }
        }
    }

    void OnApplicationQuit()
    {
        // the client/server threads won't receive the OnQuit info if we are
        // running them in the Editor. they would only quit when we press Play
        // again later. this is fine, but let's shut them down here for consistency
        client.Disconnect();
    }
}
