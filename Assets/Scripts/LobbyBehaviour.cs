using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class LobbyBehaviour : MonoBehaviour
{
    public GameObject myCamera;
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

    public void OnExit()
    {
        Application.Quit();
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
        NetworkManager.singleton.OnMatchQuit += NetworkManager_OnMatchQuit;
        NetworkManager.singleton.OnMatchEnd += NetworkManager_OnMatchEnd;

        NetworkManager.singleton.TryConnect();
    }

    private void NetworkManager_OnMatchQuit()
    {
        Restart();
        statusText.text = "";
        statusGameObject.SetActive(false);
    }

    private void NetworkManager_OnConnection(bool success)
    {
        if (!success)
        {
            statusText.text = "No se pudo conectar al servidor. Revisa tu conexion a internet.";
            Restart();
            return;
        }

        statusText.text = "Buscando una partida. Por favor espera.";
    }

    private void NetworkManager_OnMatchReady()
    {
        audioPlayer.Stop();
        videoPlayer.Stop();
        myCamera.SetActive(false);
    }

    private void NetworkManager_OnMatchEnd(bool draw, bool iWin)
    {
        myCamera.SetActive(true);
        string text;
        if (iWin && draw) text = "Empate! a segunda vuelta.";
        else
        {
            if (iWin) text = "Ganador!";
            else text = "Perdiste.";
        }
        statusText.text = text;

        Restart();
    }

    private void Restart()
    {
        SceneManager.UnloadSceneAsync(matchScene);

        if (!audioPlayer.isPlaying)
        {
            audioPlayer.time = 0;
            audioPlayer.Play();
        }
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
        myCamera.SetActive(true);
        buttonPlayGameObject.GetComponent<Button>().interactable = true;
    }
}
