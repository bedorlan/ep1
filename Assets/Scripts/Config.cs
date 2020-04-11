using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public static class Config
{
    internal static string serverHost;
    internal static int serverPort;
    internal static int matchLength;

    internal static void Load()
    {
        var textFile = Resources.Load<TextAsset>("config");
        var json = JSON.Parse(textFile.text).AsObject;

        serverHost = json["serverHost"];
        serverPort = json["serverPort"].AsInt;
        matchLength = json["matchLength"].AsInt;
    }
}
