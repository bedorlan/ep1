using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class WaitingForMatchBehaviour : MonoBehaviour
{
    public new GameObject camera;
    public GameObject buttonPlayGameObject;
    public GameObject statusGameObject;

    const string MATCH_SCENE = "MatchScene";
    private Scene matchScene;
    private Text statusText;
    private AudioSource audioPlayer;
    private VideoPlayer videoPlayer;

    private void Start()
    {
        statusText = statusGameObject.GetComponentInChildren<Text>();
        audioPlayer = GetComponentInChildren<AudioSource>();
        videoPlayer = GetComponentInChildren<VideoPlayer>();
    }

    public void OnPlay()
    {
        buttonPlayGameObject.GetComponent<Button>().interactable = false;
        statusGameObject.SetActive(true);

        LoadMatch();
    }

    private void LoadMatch()
    {
        statusText.text = "Conectando al servidor ...";

        if (!matchScene.IsValid())
        {
            var asyncLoadScene = SceneManager.LoadSceneAsync(MATCH_SCENE, LoadSceneMode.Additive);
            asyncLoadScene.completed += MatchScene_completed;
        }
        else
        {
            MatchScene_completed(null);
        }
    }

    private void MatchScene_completed(AsyncOperation operation)
    {
        matchScene = SceneManager.GetSceneByName(MATCH_SCENE);
        SceneManager.SetActiveScene(matchScene);

        NetworkManager.singleton.OnConnection += NetworkManager_OnConnection;
        NetworkManager.singleton.OnMatchReady += NetworkManager_OnMatchReady;
        NetworkManager.singleton.OnMatchEnd += NetworkManager_OnMatchEnd;

        NetworkManager.singleton.TryConnect();
    }

    private void NetworkManager_OnConnection(bool success)
    {
        if (!success)
        {
            statusText.text = "No se pudo conectar al servidor. Revisa tu conexion a internet.";
            buttonPlayGameObject.GetComponent<Button>().interactable = true;
            return;
        }

        statusText.text = "Buscando una partida. Por favor espera.";
    }

    private void NetworkManager_OnMatchReady()
    {
        audioPlayer.Stop();
        videoPlayer.Stop();
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
