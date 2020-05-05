using System;
using UnityEngine;

internal class AttestationHelper
{
  static internal string response { get; private set; }
  static internal volatile bool done = true;

  static internal void Attest(string nonce)
  {
    done = false;

    using (AndroidJavaObject Lilium = new AndroidJavaObject("com.nekolaboratory.Lilium.Lilium"))
    {
      var stringRep = Lilium.Call<string>("toString");
      if (stringRep == null)
      {
        response = "{\"no_attest_support\": true}";
        done = true;
        return;
      }

      var listener = new AttestationListener();
      listener.OnResponse += listener_OnResponse;
      Lilium.Call(
        "attest",
        "me",
        "AIzaSyAV-pxZlkYP_7GW7tDUrikQtYnA1hTfNwo",
        nonce,
        listener
      );
    }
  }

  private static void listener_OnResponse(string response)
  {
    AttestationHelper.response = response;
    done = true;
  }
}

public class AttestationListener : AndroidJavaProxy
{
  internal event Action<string> OnResponse;

  public AttestationListener() : base("com.nekolaboratory.Lilium.DefaultAttestCallback") { }

  public void onResult(string response)
  {
    OnResponse?.Invoke(response);
  }
}