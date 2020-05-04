using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public static class Config
{
  internal static string serverHost { get; private set; }
  internal static int serverPort { get; private set; }
  internal static int matchLength { get; private set; }

  internal static void Load()
  {
    var textFile = Resources.Load<TextAsset>("config");
    var json = JSON.Parse(textFile.text).AsObject;

    serverHost = json["serverHost"];
    serverPort = json["serverPort"].AsInt;
    matchLength = json["matchLength"].AsInt;
  }
}
