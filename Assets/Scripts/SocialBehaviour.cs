using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Facebook.Unity;
using UnityEngine;

public class SocialBehaviour : MonoBehaviour
{
    static internal SocialBehaviour singleton { get; private set; }

    internal string shortName { get; private set; }
    internal event Action OnLoggedIn;

    private bool? initialized;

    void Awake()
    {
        singleton = this;
        shortName = "";

        if (!FB.IsInitialized)
        {
            FB.Init(FBInitCallback);
        }
        else
        {
            FB.ActivateApp();
        }
    }

    private void FBInitCallback()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
            initialized = true;

            if (FB.IsLoggedIn)
            {
                OnLoggedIn?.Invoke();
                GetUserData();
            }
        }
        else
        {
            initialized = false;
        }
    }

    readonly List<string> permissions = new List<string>() {
        "public_profile",
        //"user_friends",
    };

    bool HasGrantedAllPermissions()
    {
        return permissions.All((it) => AccessToken.CurrentAccessToken.Permissions.Contains(it));
    }

    internal void Login()
    {
        if (FB.IsLoggedIn && HasGrantedAllPermissions()) return;
        StartCoroutine(LoginRoutine());
    }

    internal IEnumerator LoginRoutine()
    {
        yield return new WaitUntil(() => initialized.HasValue);
        if (!initialized.Value) yield break;

        FB.LogInWithReadPermissions(permissions, (result) => {
            if (FB.IsLoggedIn && HasGrantedAllPermissions())
            {
                GetUserData();
            }
        });
    }

    private void GetUserData()
    {
        FB.API("me?fields=short_name", HttpMethod.GET, (result) => {
            shortName = result.ResultDictionary["short_name"].ToString();
        });
    }
}
