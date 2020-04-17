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

        matchResultObject.GetComponent<MatchResultBehaviour>().OnFinished += MatchResult_OnFinished;
    }

    public void OnExit()
    {
        Application.Quit();
    }

    public void OnShowRanks()
    {
        Debug.Log("OnShowRanks");
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

    private void NetworkManager_OnMatchEnd(MatchResult matchResult)
    {
        SceneManager.UnloadSceneAsync(matchScene);
        lobbyObject.SetActive(false);
        matchResultObject.SetActive(true);
        myCamera.SetActive(true);

        if (!videoPlayer.isPlaying)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
        matchResultObject.GetComponent<MatchResultBehaviour>().ShowMatchResult(matchResult);
    }

    private void MatchResult_OnFinished()
    {
        Restart();
    }

    private void Restart()
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
        buttonPlayGameObject.GetComponent<Button>().interactable = true;

        statusText.text = "";
        statusGameObject.SetActive(false);
    }
}
