using System;
using System.Collections.Generic;
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
  public GameObject retryButton;

  public GameObject lobbyObject;
  public GameObject matchResultObject;
  public GameObject askForLoginObject;
  public GameObject scoresObject;

  const string MATCH_SCENE = "MatchScene";

  private Animator lobbyController;
  private Dictionary<int, Action> stateHandlers;
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
    socialBehaviour.OnLogged += (logged) =>
    {
      lobbyController.SetBool("logged", logged);
      lobbyController.SetBool("socialReady", true);
    };

    stateHandlers = new Dictionary<int, Action>() {
      {
        Animator.StringToHash("waitingSocial"), () =>
        {
          lobbyButtonsObject.SetActive(false);
        }
      },
      {
        Animator.StringToHash("waitingNetwork"), () =>
        {
          lobbyController.ResetTrigger("menu");
          lobbyController.ResetTrigger("cancel");
          lobbyController.ResetTrigger("matchQuit");
          statusText.text = "Conectando al servidor ...";
          myCamera.SetActive(true);
          GetComponent<GraphicRaycaster>().enabled = true;
          lobbyButtonsObject.SetActive(false);
          cancelButton.SetActive(false);
          retryButton.SetActive(false);
          if (matchScene.isLoaded) SceneManager.UnloadSceneAsync(matchScene);
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
          allLeaderboards.Item1 = null;
          allLeaderboards.Item2 = null;
          LoadMatchSceneAndConnect();
        }
      },
      {
        Animator.StringToHash("connectionFailed"), () =>
        {
          lobbyController.ResetTrigger("connectionFailed");
          statusText.text = "No se pudo conectar al servidor. Revisa tu conexion a internet";
          myCamera.SetActive(true);
          GetComponent<GraphicRaycaster>().enabled = true;
          ShowOnlyThisPanel(lobbyObject);
          lobbyButtonsObject.SetActive(false);
          cancelButton.SetActive(false);
          retryButton.SetActive(true);
        }
      },
      {
        Animator.StringToHash("ready"), () =>
        {
          lobbyController.ResetTrigger("menu");
          lobbyController.ResetTrigger("cancel");
          ShowOnlyThisPanel(lobbyObject);
          lobbyButtonsObject.SetActive(true);
          SetLobbyButtonsInteractable(true);
          cancelButton.SetActive(false);
          if (lobbyController.GetBool("logged"))
          {
            NetworkManager.singleton.Introduce();
            statusText.text = string.Format("Hola {0}", socialBehaviour.shortName);
          }
          else statusText.text = "";
        }
      },
      {
        Animator.StringToHash("askingForLogin"), () =>
        {
          lobbyController.ResetTrigger("showScores");
          lobbyController.ResetTrigger("playWithFriends");
          ShowOnlyThisPanel(askForLoginObject);
        }
      },
      {
        Animator.StringToHash("showScores"), () =>
        {
          ShowOnlyThisPanel(scoresObject);
          scoresObject.GetComponent<ScoresBehaviour>().ShowLoading();
          if (allLeaderboards.Item1 == null) NetworkManager.singleton.getAllLeaderboards();
          else NetworkManager_OnLeaderboardAllLoaded(allLeaderboards);
        }
      },
      {
        Animator.StringToHash("playWithAll"), () =>
        {
          lobbyController.ResetTrigger("playWithAll");
          NetworkManager.singleton.JoinAllQueue();
          statusText.text = "Buscando partida con cualquiera ...";
          SetLobbyButtonsInteractable(false);
          cancelButton.SetActive(true);
        }
      },
      {
        Animator.StringToHash("playWithFriends"), () =>
        {
          lobbyController.ResetTrigger("playWithFriends");
          NetworkManager.singleton.JoinFriendsQueue();
          statusText.text = "Buscando partida con amigos ...";
          SetLobbyButtonsInteractable(false);
          cancelButton.SetActive(true);
        }
      },
      {
        Animator.StringToHash("playing"), () =>
        {
          audioPlayer.Stop();
          videoPlayer.Stop();
          myCamera.SetActive(false);
          GetComponent<GraphicRaycaster>().enabled = false;
        }
      },
      {
        Animator.StringToHash("showMatchResults"), () =>
        {
          lobbyController.ResetTrigger("matchEnded");
          ShowOnlyThisPanel(matchResultObject);
          myCamera.SetActive(true);
          GetComponent<GraphicRaycaster>().enabled = true;
          if (!videoPlayer.isPlaying)
          {
            videoPlayer.time = 0;
            videoPlayer.Play();
          }
          matchResultObject.GetComponent<MatchResultBehaviour>().ShowWaitingForMatchResult();
        }
      },
    };
  }

  private void lobbyController_OnEnterState(int newState)
  {
    stateHandlers[newState]();
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

  private void ShowOnlyThisPanel(GameObject panel)
  {
    lobbyObject.SetActive(lobbyObject == panel);
    matchResultObject.SetActive(matchResultObject == panel);
    askForLoginObject.SetActive(askForLoginObject == panel);
    scoresObject.SetActive(scoresObject == panel);
  }

  private void LoadMatchSceneAndConnect()
  {
    if (matchScene.IsValid() && matchScene.isLoaded) return;

    var asyncLoadScene = SceneManager.LoadSceneAsync(MATCH_SCENE, LoadSceneMode.Additive);
    asyncLoadScene.completed += (AsyncOperation operation) =>
    {
      matchScene = SceneManager.GetSceneByName(MATCH_SCENE);
      SceneManager.SetActiveScene(matchScene);

      NetworkManager.singleton.OnMatchReady += () => lobbyController.SetBool("playing", true);
      NetworkManager.singleton.OnMatchQuit += () =>
      {
        lobbyController.SetTrigger("matchQuit");
        lobbyController.SetBool("playing", false);
      };
      NetworkManager.singleton.OnMatchEnd += () =>
      {
        lobbyController.SetTrigger("matchEnded");
        lobbyController.SetBool("playing", false);
      };
      NetworkManager.singleton.OnLeaderboardAllLoaded += NetworkManager_OnLeaderboardAllLoaded;
      NetworkManager.singleton.OnConnection += (success) =>
      {
        if (!success)
        {
          lobbyController.SetTrigger("connectionFailed");
          lobbyController.SetBool("playing", false);
        }
        lobbyController.SetBool("networkReady", success);
      };
      NetworkManager.singleton.OnMatchResult += (matchResult) =>
      {
        SceneManager.UnloadSceneAsync(matchScene);
        matchResultObject.GetComponent<MatchResultBehaviour>().ShowMatchResult(matchResult);
      };

      NetworkManager.singleton.TryConnect();
    };
  }

  private void NetworkManager_OnLeaderboardAllLoaded((Leaderboard, Leaderboard) leaderboards)
  {
    allLeaderboards = leaderboards;
    scoresObject.GetComponent<ScoresBehaviour>().ShowLeaderboardAll(leaderboards);
  }
}
