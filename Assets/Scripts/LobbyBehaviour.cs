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

  private Animator lobbyController;
  private SocialBehaviour socialBehaviour;
  private Scene matchScene;
  private Text statusText;
  private AudioSource audioPlayer;
  private VideoPlayer videoPlayer;
  private (Leaderboard, Leaderboard) allLeaderboards;

  private void Awake()
  {
    lobbyController = GetComponent<Animator>();
    socialBehaviour = GetComponent<SocialBehaviour>();
    statusText = statusGameObject.GetComponentInChildren<Text>();
    audioPlayer = GetComponentInChildren<AudioSource>();
    videoPlayer = GetComponentInChildren<VideoPlayer>();

    lobbyController.GetBehaviour<GenericTransitionStateBehaviour>().OnEnterState += lobbyController_OnEnterState;
    socialBehaviour.OnLogged += SocialBehaviour_OnLogged;
    matchResultObject.GetComponent<MatchResultBehaviour>().OnFinished += MatchResult_OnFinished;
  }

  private void lobbyController_OnEnterState(string newState)
  {
    Debug.Log("newState=" + newState);
    switch (newState)
    {
      case "waitingSocial":
        lobbyButtonsObject.SetActive(false);
        break;
      case "waitingNetwork":
        lobbyButtonsObject.SetActive(false);
        cancelButton.SetActive(false);
        Restart();
        break;
      case "ready":
        lobbyController.ResetTrigger("menu");
        lobbyController.ResetTrigger("cancel");
        ShowOnlyThisPanel(lobbyObject);
        lobbyButtonsObject.SetActive(true);
        SetLobbyButtonsInteractable(true);
        cancelButton.SetActive(false);
        break;
      case "askingForLogin":
        lobbyController.ResetTrigger("showScores");
        AskForLogin();
        break;
      case "showScores":
        ShowOnlyThisPanel(scoresObject);
        scoresObject.GetComponent<ScoresBehaviour>().ShowLoading();
        if (allLeaderboards.Item1 == null) NetworkManager.singleton.getAllLeaderboards();
        else NetworkManager_OnLeaderboardAllLoaded(allLeaderboards);
        break;
      case "playWithAll":
        lobbyController.ResetTrigger("playWithAll");
        NetworkManager.singleton.JoinAllQueue();
        statusText.text = "Buscando partida con cualquiera ...";
        SetLobbyButtonsInteractable(false);
        cancelButton.SetActive(true);
        break;
      case "playing":
        audioPlayer.Stop();
        videoPlayer.Stop();
        myCamera.SetActive(false);
        GetComponent<GraphicRaycaster>().enabled = false;
        break;
    }
  }

  private void SocialBehaviour_OnLogged(bool logged)
  {
    lobbyController.SetBool("logged", logged);
    lobbyController.SetBool("socialReady", true);
    if (logged)
    {
      NetworkManager.singleton.Introduce();
      statusText.text = string.Format("Hola {0}", socialBehaviour.shortName);
    }

  }

  public void OnExit()
  {
    Application.Quit();
  }

  private void SetLobbyButtonsInteractable(bool interactable)
  {
    foreach (var button in lobbyButtonsObject.GetComponentsInChildren<Button>())
    {
      button.interactable = interactable;
    }
  }

  public void OnUserWantsToLogin()
  {
    socialBehaviour.Login();
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

  private void ShowOnlyThisPanel(GameObject panel)
  {
    lobbyObject.SetActive(lobbyObject == panel);
    matchResultObject.SetActive(matchResultObject == panel);
    askForLoginObject.SetActive(askForLoginObject == panel);
    scoresObject.SetActive(scoresObject == panel);
  }

  private void AskForLogin()
  {
    ShowOnlyThisPanel(askForLoginObject);
  }

  public void OnPlayWithFriends()
  {
    if (!TryLogin()) return;

    statusText.text = "Buscando partida con amigos ...";
    // NetworkManager.singleton.JoinFriendsQueue();
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
      NetworkManager.singleton.OnMatchReady += () => lobbyController.SetBool("playing", true);
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
      Restart(false);
      return;
    }

    statusText.text = "Conectado";
    lobbyController.SetBool("networkReady", true);
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
    ShowOnlyThisPanel(matchResultObject);
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
    ShowOnlyThisPanel(lobbyObject);

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

    if (clearStatusText)
    {
      statusText.text = "";
    }

    allLeaderboards.Item1 = null;
    allLeaderboards.Item2 = null;

    LoadMatchSceneAndConnect();
  }
}
