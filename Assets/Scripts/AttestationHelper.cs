using System;
using System.Threading;
using SimpleJSON;
using UnityEngine;

internal class AttestationHelper
{
  static internal string response { get; private set; }
  static internal volatile bool done = true;

  static internal void Attest(string nonce, int tries = 1)
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

      var listener = new AttestationListener(nonce, tries);
      listener.OnResponse += listener_OnResponse;
      Lilium.Call(
        "attest",
        "me",
        "",
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

  private string nonce;
  private int tries;

  public AttestationListener(string nonce, int tries) : base("com.nekolaboratory.Lilium.DefaultAttestCallback")
  {
    this.nonce = nonce;
    this.tries = tries;
  }

  public void onResult(string response)
  {
    var data = JSON.Parse(response).AsObject;
    var atnError = (string)data["atn_error"];
    if (tries < 3 && atnError == "ATTEST_API_ERROR_NETWORK_ERROR")
    {
      Thread.Sleep(2000);
      AttestationHelper.Attest(nonce, tries + 1);
      return;
    }

    data.Add("tries", tries);
    OnResponse?.Invoke(data.ToString());
  }
}