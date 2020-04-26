using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class LobbyBehaviour : MonoBehaviour
{
  public GameObject myCamera;
  public GameObject buttonPlayGameObject;
  public GameObject buttonRanks;
  public GameObject statusGameObject;
  public GameObject lobbyObject;
  public GameObject matchResultObject;

  const string MATCH_SCENE = "MatchScene";

  private SocialBehaviour socialBehaviour;
  private Scene matchScene;
  private Text statusText;
  private AudioSource audioPlayer;
  private VideoPlayer videoPlayer;

  private void Start()
  {
    socialBehaviour = GetComponent<SocialBehaviour>();
    statusText = statusGameObject.GetComponentInChildren<Text>();
    audioPlayer = GetComponentInChildren<AudioSource>();
    videoPlayer = GetComponentInChildren<VideoPlayer>();

    socialBehaviour.OnLogged += OnLogged;
    matchResultObject.GetComponent<MatchResultBehaviour>().OnFinished += MatchResult_OnFinished;
  }

  private void OnLogged()
  {
    statusGameObject.SetActive(true);
    statusText.text = string.Format("Hola {0}", socialBehaviour.shortName);
  }

  public void OnExit()
  {
    Application.Quit();
  }

  public void OnShowRanks()
  {
    socialBehaviour.Login();
  }

  public void OnPlay()
  {
    buttonPlayGameObject.GetComponentInChildren<Button>().interactable = false;
    buttonRanks.GetComponentInChildren<Button>().interactable = false;
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
    NetworkManager.singleton.OnMatchResult += NetworkManager_OnMatchResults;

    NetworkManager.singleton.TryConnect();
  }

  private void NetworkManager_OnMatchQuit()
  {
    Restart();
  }

  private void NetworkManager_OnConnection(bool success)
  {
    if (!success)
    {
      statusText.text = "No se pudo conectar al servidor. Revisa tu conexion a internet.";
      Restart(false);
      return;
    }

    statusText.text = "Buscando una partida. Por favor espera.";
  }

  private void NetworkManager_OnMatchReady()
  {
    audioPlayer.Stop();
    videoPlayer.Stop();
    myCamera.SetActive(false);
    GetComponent<GraphicRaycaster>().enabled = false;
  }

  private void NetworkManager_OnMatchEnd()
  {
    lobbyObject.SetActive(false);
    myCamera.SetActive(true);
    GetComponent<GraphicRaycaster>().enabled = true;

    if (!videoPlayer.isPlaying)
    {
      videoPlayer.time = 0;
      videoPlayer.Play();
    }

    matchResultObject.GetComponent<MatchResultBehaviour>().ShowWaitingForMatchResult();
    matchResultObject.SetActive(true);
  }

  private void NetworkManager_OnMatchResults(MatchResult matchResult)
  {
    SceneManager.UnloadSceneAsync(matchScene);
    matchResultObject.GetComponent<MatchResultBehaviour>().ShowMatchResult(matchResult);
  }

  private void MatchResult_OnFinished()
  {
    Restart();
  }

  private void Restart(bool clearStatusText = true)
  {
    if (matchScene.isLoaded) SceneManager.UnloadSceneAsync(matchScene);
    lobbyObject.SetActive(true);
    matchResultObject.SetActive(false);

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
    GetComponent<GraphicRaycaster>().enabled = true;
    buttonPlayGameObject.GetComponent<Button>().interactable = true;
    buttonRanks.GetComponentInChildren<Button>().interactable = true;

    if (clearStatusText)
    {
      statusText.text = "";
      statusGameObject.SetActive(false);
    }
  }
}
