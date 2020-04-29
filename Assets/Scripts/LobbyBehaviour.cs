using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class LobbyBehaviour : MonoBehaviour
{
  public GameObject myCamera;

  public GameObject lobbyButtonsObject;
  public GameObject buttonPlayGameObject;
  public GameObject buttonRanks;
  public GameObject statusGameObject;

  public GameObject lobbyObject;
  public GameObject matchResultObject;
  public GameObject askForLoginObject;
  public GameObject scoresObject;

  const string MATCH_SCENE = "MatchScene";

  private SocialBehaviour socialBehaviour;
  private Scene matchScene;
  private Text statusText;
  private AudioSource audioPlayer;
  private VideoPlayer videoPlayer;
  private (Leaderboard, Leaderboard) allLeaderboards;

  private void Awake()
  {
    socialBehaviour = GetComponent<SocialBehaviour>();
    statusText = statusGameObject.GetComponentInChildren<Text>();
    audioPlayer = GetComponentInChildren<AudioSource>();
    videoPlayer = GetComponentInChildren<VideoPlayer>();

    socialBehaviour.OnLogged += SocialBehaviour_OnLogged;
    matchResultObject.GetComponent<MatchResultBehaviour>().OnFinished += MatchResult_OnFinished;
  }

  private void Start()
  {
    LoadMatch();
  }

  private void SocialBehaviour_OnLogged(bool logged)
  {
    if (askForLoginObject.activeSelf && logged) OnLobbyMenu();
    if (logged)
    {
      NetworkManager.singleton.Introduce();
      statusText.text = string.Format("Hola {0}", socialBehaviour.shortName);
    }

    if (!lobbyButtonsObject.activeSelf)
    {
      lobbyButtonsObject.SetActive(true);
    }
  }

  public void OnExit()
  {
    Application.Quit();
  }

  public void OnUserWantsToLogin()
  {
    socialBehaviour.Login();
  }

  public void OnShowScores()
  {
    if (!TryLogin()) return;

    HideAllPanels();
    scoresObject.SetActive(true);
    scoresObject.GetComponent<ScoresBehaviour>().ShowLoading();
    if (allLeaderboards.Item1 == null) NetworkManager.singleton.getAllLeaderboards();
    else NetworkManager_OnLeaderboardAllLoaded(allLeaderboards);
  }

  private void NetworkManager_OnLeaderboardAllLoaded((Leaderboard, Leaderboard) leaderboards)
  {
    allLeaderboards = leaderboards;
    scoresObject.GetComponent<ScoresBehaviour>().ShowLeaderboardAll(leaderboards);
  }

  private bool TryLogin()
  {
    if (socialBehaviour.userId != "") return true;

    AskForLogin();
    return false;
  }

  private void HideAllPanels()
  {
    lobbyObject.SetActive(false);
    matchResultObject.SetActive(false);
    askForLoginObject.SetActive(false);
    scoresObject.SetActive(false);
  }

  private void AskForLogin()
  {
    HideAllPanels();
    askForLoginObject.SetActive(true);
  }

  public void OnLobbyMenu()
  {
    HideAllPanels();
    lobbyObject.SetActive(true);
  }

  public void OnPlay()
  {
    buttonPlayGameObject.GetComponentInChildren<Button>().interactable = false;
    buttonRanks.GetComponentInChildren<Button>().interactable = false;

    LoadMatch();
  }

  public void OnPlayWithFriends()
  {
    if (!TryLogin()) return;
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
    NetworkManager.singleton.OnLeaderboardAllLoaded += NetworkManager_OnLeaderboardAllLoaded;

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

    statusText.text = "Conectado";
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
    myCamera.SetActive(true);
    GetComponent<GraphicRaycaster>().enabled = true;

    if (!videoPlayer.isPlaying)
    {
      videoPlayer.time = 0;
      videoPlayer.Play();
    }

    matchResultObject.GetComponent<MatchResultBehaviour>().ShowWaitingForMatchResult();
    HideAllPanels();
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
    HideAllPanels();
    lobbyObject.SetActive(true);

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
    buttonPlayGameObject.GetComponentInChildren<Button>().interactable = true;
    buttonRanks.GetComponentInChildren<Button>().interactable = true;

    if (clearStatusText)
    {
      statusText.text = "";
    }

    allLeaderboards.Item1 = null;
    allLeaderboards.Item2 = null;
  }
}
