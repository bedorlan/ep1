using UnityEngine;

internal class AttestationHelper
{
  static internal void Attest()
  {
    Debug.Log("Attest");
    using (AndroidJavaObject Lilium = new AndroidJavaObject("com.nekolaboratory.Lilium.Lilium"))
    {
      Debug.Log("Lilium.Call");
      Lilium.Call(
        "attest",
        "com.bedorlan.ep1.user",
        "AIzaSyAV-pxZlkYP_7GW7tDUrikQtYnA1hTfNwo",
        "1234567890123456",
        new AttestationListener()
      );
    }
  }
}

public class AttestationListener : AndroidJavaProxy
{
  public AttestationListener() : base("com.nekolaboratory.Lilium.DefaultAttestCallback")
  {
    Debug.Log("new AttestationHelper");
  }

  //todo: Using the callback function in Unity Android Plugin means that the main thread ID is changed after the event fires.
  //Please consider how to return to the main thread.
  public void onResult(string response)
  {
    Debug.Log("onResult");
    Debug.Log(response);
  }
}