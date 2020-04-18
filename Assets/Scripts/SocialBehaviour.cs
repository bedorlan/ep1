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

    private bool? initialized;

    void Awake()
    {
        singleton = this;
        shortName = "";

        if (!FB.IsInitialized) FB.Init(FBInitCallback);
        else FBInitCallback();
    }

    private void FBInitCallback()
    {
        initialized = FB.IsInitialized;
        if (!FB.IsInitialized) return;

        FB.ActivateApp();
        LoginCallback();
    }

    void LoginCallback()
    {
        if (FB.IsLoggedIn && HasGrantedAllPermissions())
        {
            GetUserData();
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

    IEnumerator LoginRoutine()
    {
        yield return new WaitUntil(() => initialized.HasValue);
        if (!initialized.Value) yield break;

        FB.LogInWithReadPermissions(permissions, (result) => {
            if (result.Error != null)
            {
                Debug.LogError(result.Error);
                return;
            }
            LoginCallback();
        });
    }

    private void GetUserData()
    {
        FB.API("me?fields=short_name", HttpMethod.GET, (result) => {
            if (result.Error != null)
            {
                Debug.LogError(result.Error);
                return;
            }
            shortName = result.ResultDictionary["short_name"].ToString();
        });
    }
}
