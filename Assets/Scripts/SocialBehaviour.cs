using System;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;
using UnityEngine;

public class SocialBehaviour : MonoBehaviour
{
    private bool? initialized;
    private AccessToken aToken;

    internal event Action OnLoggedIn;

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

            if (FB.IsLoggedIn) OnLoggedIn?.Invoke();
        }
        else
        {
            initialized = false;
        }
    }

    internal void Login()
    {
        StartCoroutine(LoginRoutine());
    }

    internal IEnumerator LoginRoutine()
    {
        yield return new WaitUntil(() => initialized.HasValue);
        if (!initialized.Value) yield break;

        var perms = new List<string>() { "public_profile", "user_friends" };
        FB.LogInWithReadPermissions(perms, AuthCallback);
    }

    private void AuthCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            aToken = AccessToken.CurrentAccessToken;
            Debug.Log(aToken.UserId);
            foreach (string perm in aToken.Permissions)
            {
                Debug.Log(perm);
            }
        }
        else
        {
            Debug.Log("User cancelled login");
        }
    }
}
