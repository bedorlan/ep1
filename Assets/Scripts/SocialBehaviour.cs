using System;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;
using UnityEngine;

public class SocialBehaviour : MonoBehaviour
{
    internal string shortName { get; private set; }
    internal event Action OnLoggedIn;

    private bool? initialized;
    private AccessToken aToken;

    void Awake()
    {
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

    internal void Login()
    {
        if (FB.IsLoggedIn) return;
        StartCoroutine(LoginRoutine());
    }

    internal IEnumerator LoginRoutine()
    {
        yield return new WaitUntil(() => initialized.HasValue);
        if (!initialized.Value) yield break;

        var perms = new List<string>() {
            "public_profile",
            //"user_friends",
        };
        FB.LogInWithReadPermissions(perms, (result) => {
            if (FB.IsLoggedIn)
            {
                aToken = AccessToken.CurrentAccessToken;
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
