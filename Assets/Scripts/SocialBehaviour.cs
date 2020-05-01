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

  void Start()
  {
    singleton = this;
    shortName = "";
    userId = "";

    if (!FB.IsInitialized)
    {
      try
      {
        FB.Init(FBInitCallback);
      }
      catch (NotSupportedException)
      {
        Debug.Log("FB not supported");
        OnLogged?.Invoke(false);
      }
    }
    else FBInitCallback();
  }

  private void FBInitCallback()
  {
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
    if (FB.IsLoggedIn && HasGrantedAllPermissions())
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
    LoginRoutine();
  }

  void LoginRoutine()
  {
    if (!FB.IsInitialized) return;

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
  }
}
