using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaitingForMatchBehaviour : MonoBehaviour
{
    public new GameObject camera;

    const string MATCH_SCENE = "MatchScene";

    void Start()
    {
        LoadMatch();
    }

    private Scene matchScene;

    private void LoadMatch()
    {
        var asyncLoadScene = SceneManager.LoadSceneAsync(MATCH_SCENE, LoadSceneMode.Additive);
        asyncLoadScene.completed += MatchScene_completed;
    }

    private void MatchScene_completed(AsyncOperation operation)
    {
        matchScene = SceneManager.GetSceneByName(MATCH_SCENE);
        SceneManager.SetActiveScene(matchScene);

        NetworkManager.singleton.OnMatchReady += NetworkManager_OnMatchReady;
        NetworkManager.singleton.OnMatchEnd += NetworkManager_OnMatchEnd;
    }

    private void NetworkManager_OnMatchReady()
    {
        camera.SetActive(false);
    }

    private void NetworkManager_OnMatchEnd()
    {
        camera.SetActive(true);
        SceneManager.UnloadSceneAsync(matchScene).completed += MatchScene_unload;
    }

    private void MatchScene_unload(AsyncOperation obj)
    {
        LoadMatch();
    }
}
