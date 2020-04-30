using System;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class LobbyBehaviour : MonoBehaviour
{
  public GameObject myCamera;

  public GameObject statusGameObject;
  public GameObject lobbyButtonsObject;
  public GameObject cancelButton;

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

  private bool socialReady = false;
  private bool networkReady = false;

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
    LoadMatchSceneAndConnect();
  }

  private void SocialBehaviour_OnLogged(bool logged)
  {
    if (askForLoginObject.activeSelf && logged) OnLobbyMenu();
    if (logged)
    {
      NetworkManager.singleton.Introduce();
      statusText.text = string.Format("Hola {0}", socialBehaviour.shortName);
    }

    socialReady = true;
    TryEnableLobbyButtons();
  }

  private void TryEnableLobbyButtons()
  {
    lobbyButtonsObject.SetActive(everythingIsReady);
  }

  private bool everythingIsReady
  {
    get
    {
      return socialReady && matchScene.IsValid() && matchScene.isLoaded && networkReady;
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
    statusText.text = "Buscando partida con cualquiera ...";
    setBusyMode(true);
    NetworkManager.singleton.JoinAllQueue();
  }

  public void OnPlayWithFriends()
  {
    if (!TryLogin()) return;

    statusText.text = "Buscando partida con amigos ...";
    setBusyMode(true);
    // NetworkManager.singleton.JoinFriendsQueue();
  }

  private void setBusyMode(bool busy)
  {
    cancelButton.SetActive(busy);
    foreach (var button in lobbyButtonsObject.GetComponentsInChildren<Button>())
    {
      button.interactable = !busy;
    }
  }

  public void OnCancel()
  {
    Debug.Log("OnCancel");
    // todo
  }

  private void LoadMatchSceneAndConnect()
  {
    if (matchScene.IsValid() && matchScene.isLoaded) return;

    statusText.text = "Conectando al servidor ...";
    var asyncLoadScene = SceneManager.LoadSceneAsync(MATCH_SCENE, LoadSceneMode.Additive);
    asyncLoadScene.completed += (AsyncOperation operation) =>
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
    };
  }

  private void NetworkManager_OnMatchQuit()
  {
    Restart();
  }

  private void NetworkManager_OnConnection(bool success)
  {
    if (!success)
    {
      networkReady = false;
      TryEnableLobbyButtons();
      Restart(false);
      return;
    }

    statusText.text = "Conectado";
    networkReady = true;
    TryEnableLobbyButtons();
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
    networkReady = false;
    TryEnableLobbyButtons();
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
    setBusyMode(false);

    if (clearStatusText)
    {
      statusText.text = "";
    }

    allLeaderboards.Item1 = null;
    allLeaderboards.Item2 = null;

    LoadMatchSceneAndConnect();
  }
}
