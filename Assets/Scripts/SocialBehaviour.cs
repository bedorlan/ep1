using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facebook.Unity;
using SimpleJSON;
using UnityEngine;

public class SocialBehaviour : MonoBehaviour
{
  static internal SocialBehaviour singleton { get; private set; }

  internal string shortName { get; private set; }
  internal string userId { get; private set; }

  readonly internal Queue<String> errors = new Queue<String>();
  internal event Action<bool> OnLogged;
  internal event Action OnError;

  private bool? initialized;

  void Awake()
  {
    singleton = this;
    shortName = "";
    userId = "";

    if (!FB.IsInitialized) FB.Init(FBInitCallback);
    else FBInitCallback();
  }

  private void FBInitCallback()
  {
    initialized = FB.IsInitialized;
    if (!FB.IsInitialized)
    {
      OnLogged?.Invoke(false);
      return;
    }

    FB.ActivateApp();
    LoginCallback();
  }

  void LoginCallback()
  {
    if (FB.IsLoggedIn)
    {
      userId = AccessToken.CurrentAccessToken.UserId;
      GetUserData();
    }
    else
    {
      OnLogged?.Invoke(false);
    }
  }

  readonly List<string> permissions = new List<string>() {
        "public_profile",
        "user_friends",
    };

  bool PermissionPublicProfile
  {
    get
    {
      return AccessToken.CurrentAccessToken.Permissions.Contains("public_profile");
    }
  }

  bool PermissionUserFriends
  {
    get
    {
      return AccessToken.CurrentAccessToken.Permissions.Contains("user_friends");
    }
  }

  bool HasGrantedAllPermissions()
  {
    return PermissionPublicProfile && PermissionUserFriends;
  }

  internal void Login()
  {
    if (FB.IsLoggedIn && HasGrantedAllPermissions()) return;
    StartCoroutine(LoginRoutine());
  }

  IEnumerator LoginRoutine()
  {
    yield return new WaitUntil(() => initialized.HasValue);
    if (!initialized.Value) yield break;

    FB.LogInWithReadPermissions(permissions, (result) =>
    {
      if (result.Error != null)
      {
        errors.Enqueue(result.Error);
        OnError?.Invoke();
        return;
      }
      LoginCallback();
    });
  }

  private void GetUserData()
  {
    if (PermissionPublicProfile)
    {
      FB.API("/me?fields=short_name", HttpMethod.GET, (result) =>
      {
        if (result.Error != null)
        {
          errors.Enqueue(result.Error);
          OnError?.Invoke();
          return;
        }
        shortName = result.ResultDictionary["short_name"].ToString();
        OnLogged?.Invoke(true);
      });
    }

    if (PermissionUserFriends)
    {
      FB.API("/me/friends", HttpMethod.GET, (result) =>
      {
        if (result.Error != null)
        {
          errors.Enqueue(result.Error);
          OnError?.Invoke();
          return;
        }
        Debug.Log(result.RawResult);
        var data = JSON.Parse(result.RawResult)["data"].AsArray;
      });
    }
    else
    {
      Debug.Log("no friends permission.");
    }
  }
}
