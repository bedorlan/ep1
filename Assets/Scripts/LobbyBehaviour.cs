using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
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
          lobbyController.SetBool("networkReady", false);
          lobbyController.SetBool("attested", false);
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
          lobbyController.ResetTrigger("menu");
          lobbyController.ResetTrigger("cancel");
          lobbyController.ResetTrigger("matchQuit");
        }
      },
      {
        Animator.StringToHash("waitingAttest"), () =>
        {
          StartCoroutine(DoAttest());
        }
      },
      {
        Animator.StringToHash("connectionFailed"), () =>
        {
          statusText.text = "No se pudo conectar al servidor. Revisa tu conexion a internet";
          myCamera.SetActive(true);
          GetComponent<GraphicRaycaster>().enabled = true;
          ShowOnlyThisPanel(lobbyObject);
          lobbyButtonsObject.SetActive(false);
          cancelButton.SetActive(false);
          retryButton.SetActive(true);
          lobbyController.ResetTrigger("connectionFailed");
        }
      },
      {
        Animator.StringToHash("ready"), () =>
        {
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
          lobbyController.ResetTrigger("menu");
          lobbyController.ResetTrigger("cancel");
        }
      },
      {
        Animator.StringToHash("askingForLogin"), () =>
        {
          ShowOnlyThisPanel(askForLoginObject);
          lobbyController.ResetTrigger("showScores");
          lobbyController.ResetTrigger("playWithFriends");
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
          NetworkManager.singleton.JoinAllQueue();
          statusText.text = "Buscando partida con cualquiera ...";
          SetLobbyButtonsInteractable(false);
          cancelButton.SetActive(true);
          lobbyController.ResetTrigger("playWithAll");
        }
      },
      {
        Animator.StringToHash("playWithFriends"), () =>
        {
          NetworkManager.singleton.JoinFriendsQueue();
          statusText.text = "Buscando partida con amigos ...";
          SetLobbyButtonsInteractable(false);
          cancelButton.SetActive(true);
          lobbyController.ResetTrigger("playWithFriends");
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
          ShowOnlyThisPanel(matchResultObject);
          myCamera.SetActive(true);
          GetComponent<GraphicRaycaster>().enabled = true;
          if (!videoPlayer.isPlaying)
          {
            videoPlayer.time = 0;
            videoPlayer.Play();
          }
          lobbyController.ResetTrigger("matchEnded");
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
      NetworkManager.singleton.OnAttestationResult += () => lobbyController.SetBool("attested", true);
      NetworkManager.singleton.OnMatchResult += (matchResult) =>
      {
        SceneManager.UnloadSceneAsync(matchScene);
        matchResultObject.GetComponent<MatchResultBehaviour>().ShowMatchResult(matchResult);
      };

      NetworkManager.singleton.TryConnect();
    };
  }

  private IEnumerator DoAttest()
  {
#if !UNITY_EDITOR && UNITY_ANDROID
    var nonce = NetworkManager.singleton.nonce;
    AttestationHelper.Attest(nonce);
    yield return new WaitUntil(() => AttestationHelper.done);

    var attestResponse = AttestationHelper.response;
    NetworkManager.singleton.Attest(attestResponse);

#else
    lobbyController.SetBool("attested", true);
    yield break;

#endif
  }

  private void NetworkManager_OnLeaderboardAllLoaded((Leaderboard, Leaderboard) leaderboards)
  {
    allLeaderboards = leaderboards;
    scoresObject.GetComponent<ScoresBehaviour>().ShowLeaderboardAll(leaderboards);
  }
}
