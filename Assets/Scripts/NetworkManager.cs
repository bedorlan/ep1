using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SimpleJSON;
using UnityEngine;

enum Codes
{
    noop = 0, // [0]
    start = 1, // [1, playerNumber: int]
    newPlayerDestination = 2, // [2, positionX: float, timeWhenReach: long]
    newVoters = 3, // [3, ...voters: [id: int, positionX: float]]
    guessTime = 5, // to server: [5, guessedTime: int], from server: [5, deltaGuess: int]
    projectileFired = 6, // [(6), (player: int), (destinationVector: [x, y: floats]), (timeWhenReach: long), (projectileType: int)]
    tryConvertVoter = 7, // [(7), (voterId: int), (player: int), (time: long)]
    voterConverted = 8, // [(8), (voterId: int), (player: int)]
    tryClaimVoter = 9, // [(9), (voterId: int)]
    voterClaimed = 10, // [(10), (voterId: int), (player: int)]
    hello = 11, // [(11)]
    // if code == 50 .. be careful: ctrl + f Codes.guessTime on server
}

public class NetworkManager : MonoBehaviour
{
    const float FLOOR_LEVEL_Y = -4f;

    public static NetworkManager singleton;

    public GameObject playerPrefab;
    public GameObject voterPrefab;
    public new GameObject camera;
    public GameObject background;
    public List<GameObject> projectilePrefabs;
    public List<GameObject> votesCounters;

    private int playerNumber;
    private Dictionary<Codes, Action<JSONNode>> codesMap;
    private Dictionary<int, GameObject> projectilesMap;
    private GameObject defaultProjectile;

    private GameObject localPlayer;
    private GameObject remotePlayer;

    void Awake()
    {
        singleton = this;

        // update even if window isn't focused, otherwise we don't receive.
        Application.runInBackground = true;

        // use Debug.Log functions for Telepathy so we can see it in the console
        Telepathy.Logger.Log = Debug.Log;
        Telepathy.Logger.LogWarning = Debug.LogWarning;
        Telepathy.Logger.LogError = Debug.LogError;

        codesMap = new Dictionary<Codes, Action<JSONNode>> {
            { Codes.start, StartGame },
            { Codes.newPlayerDestination, OnRemoteNewDestination },
            { Codes.newVoters, OnNewVoters },
            { Codes.guessTime, OnGuessTime },
            { Codes.projectileFired, OnProjectileFired },
            { Codes.voterConverted, OnVoterConverted },
            { Codes.voterClaimed, OnVoterClaimed },
            { Codes.hello, OnHello },
        };

        projectilesMap = new Dictionary<int, GameObject>();
        foreach (var projectilePrefab in projectilePrefabs)
        {
            var typeId = projectilePrefab.GetComponentInChildren<ButtonProjectileBehaviour>().GetProjectileTypeId();
            projectilesMap.Add(typeId, projectilePrefab);
        }

        defaultProjectile = projectilesMap[0].GetComponentInChildren<ButtonProjectileBehaviour>().projectilePrefab;
    }

    private Telepathy.Client client;
    public event Action OnMatchReady;
    public event Action OnMatchEnd;

    void Start()
    {
        client = new Telepathy.Client();

#if UNITY_EDITOR || !UNITY_ANDROID
        client.Connect("localhost", 7777);
#else
        client.Connect("3.223.135.88", 80);
#endif

        if (OnMatchReady?.GetInvocationList().Length > 0)
        {
            camera.SetActive(false);
        }
    }

    void Update()
    {
        Telepathy.Message msg;
        while (client.GetNextMessage(out msg))
        {
            switch (msg.eventType)
            {
                case Telepathy.EventType.Connected:
                    Debug.Log("Connected");
                    sayHello();
                    break;
                case Telepathy.EventType.Data:
                    ProcessRemoteMsg(msg.data);
                    break;
                case Telepathy.EventType.Disconnected:
                    Debug.Log("Disconnected");
                    camera.SetActive(false);
                    OnMatchEnd?.Invoke();
                    break;
            }
        }
    }

    private void sayHello()
    {
        var msg = string.Format("[{0}]", (int)Codes.hello);
        SendNetworkMsg(msg);
    }

    #region remote events

    private void ProcessRemoteMsg(byte[] data)
    {
        var asciiData = Encoding.ASCII.GetString(data);
        var jsonData = JSON.Parse(asciiData);
        Codes code = (Codes)jsonData[0].AsInt;
        if (!codesMap.ContainsKey(code))
        {
            Debug.LogError("unknown message=" + asciiData);
        }
        codesMap[code].Invoke(jsonData);
    }

    private void OnHello(JSONNode data)
    {
        // yay!
        // should i validate the server somehow?

        guessServerTime();
    }

    private void StartGame(JSONNode data)
    {
        playerNumber = data[1].AsInt;
        Debug.Log("myPlayer=" + playerNumber);

        localPlayer = Instantiate(playerPrefab);
        camera.GetComponent<MainCamera>().objectToFollow = localPlayer;
        localPlayer.GetComponent<PlayerBehaviour>().Initialize(playerNumber, true);

        remotePlayer = Instantiate(playerPrefab);
        remotePlayer.GetComponent<PlayerBehaviour>().Initialize(playerNumber == 0 ? 1 : 0, false);

        ignoreCollisions();

        votesCounters[0].GetComponent<VotesCountBehaviour>().SetVotes(0);
        votesCounters[1].GetComponent<VotesCountBehaviour>().SetVotes(0);
        votesCounters[2].SetActive(false);
        votesCounters[3].SetActive(false);

        OnMatchReady?.Invoke();
        camera.SetActive(true);

        ProjectileSelected(defaultProjectile);

        TimerBehaviour.singleton.StartTimer();
    }

    private void OnRemoteNewDestination(JSONNode data)
    {
        var newDestination = data[1].AsFloat;
        var timeWhenReach = data[2].AsLong;
        var timeToReach = timeWhenReach2timeToReach(timeWhenReach);
        remotePlayer.GetComponent<PlayerBehaviour>().Remote_NewDestination(newDestination, timeToReach);
    }

    private Dictionary<int, GameObject> votersMap = new Dictionary<int, GameObject>();

    private void OnNewVoters(JSONNode data)
    {
        data.Remove(0);
        foreach (var voter in data.Values)
        {
            var voterId = voter[0].AsInt;
            var voterPositionX = voter[1].AsFloat;

            var newVoter = Instantiate(voterPrefab);
            newVoter.GetComponent<VoterBehaviour>().SetId(voterId);
            newVoter.transform.position = new Vector3(voterPositionX, FLOOR_LEVEL_Y + .3f, 0f);

            votersMap.Add(voterId, newVoter);
        }
    }

    private void OnProjectileFired(JSONNode data)
    {
        // [(6), (player: int), (destinationVector: [x, y: floats]), (timeWhenReach: long), (projectileType: int)]
        var destination = new Vector3(data[2].AsArray[0].AsFloat, data[2].AsArray[1].AsFloat, 0f);
        var timeWhenReach = data[3].AsLong;
        var timeToReach = timeWhenReach2timeToReach(timeWhenReach);
        var projectileType = data[4].AsInt;
        var projectile = projectilesMap[projectileType].GetComponentInChildren<ButtonProjectileBehaviour>().projectilePrefab;
        remotePlayer.GetComponent<PlayerBehaviour>().Remote_FireProjectile(projectile, destination, timeToReach);
    }

    private void OnVoterConverted(JSONNode data)
    {
        var voterId = data[1].AsInt;
        var player = data[2].AsInt;
        var voter = votersMap[voterId];
        voter.GetComponent<VoterBehaviour>().ConvertTo(player);
    }

    private void OnVoterClaimed(JSONNode data)
    {
        var voterId = data[1].AsInt;
        var player = data[2].AsInt;
        var voter = votersMap[voterId];
        voter.GetComponent<VoterBehaviour>().ClaimedBy(player);

        votesCounters[player].GetComponent<VotesCountBehaviour>().PlusOneVote();
    }

    #endregion

    private void ignoreCollisions()
    {
        Physics2D.IgnoreCollision(localPlayer.GetComponent<Collider2D>(), background.GetComponent<Collider2D>());
        Physics2D.IgnoreCollision(remotePlayer.GetComponent<Collider2D>(), background.GetComponent<Collider2D>());
        Physics2D.IgnoreCollision(localPlayer.GetComponent<Collider2D>(), remotePlayer.GetComponent<Collider2D>());
    }

    private long timer = -1;
    private int minServerLatency = int.MaxValue;
    private int serverDelta = 0;

    private void guessServerTime()
    {
        var now = timer = unixMillis();
        var msg = string.Format("[{0}, {1}]", (int)Codes.guessTime, now);
        SendNetworkMsg(msg);
    }

    private void OnGuessTime(JSONNode data)
    {
        var newDelta = data[1].AsInt;
        StartCoroutine(CalcDelta(newDelta));
    }

    private IEnumerator CalcDelta(int newDelta)
    {
        if (timer == -1) yield break;

        var newLatency = (int)(unixMillis() - timer);
        timer = -1;
        if (newLatency < minServerLatency)
        {
            serverDelta = newDelta - (newLatency / 2);
            minServerLatency = newLatency;
            Debug.LogFormat("serverDelta={0} minServerLatency={1}", serverDelta, minServerLatency);
        }

        if (minServerLatency <= 40) yield break;

        yield return new WaitForSeconds(1f);
        guessServerTime();
    }

    #region local events

    public void BackgroundClicked(Vector3 position)
    {
        var playerBehaviour = localPlayer.GetComponent<PlayerBehaviour>();
        playerBehaviour?.OnNewDestination(position.x);
    }

    public void NewLocalPlayerDestination(float newDestinationX, float timeToReach)
    {
        var timeWhenReach = timeToReach2timeWhenReach(timeToReach);
        var msg = string.Format("[{0}, {1}, {2}]", (int)Codes.newPlayerDestination, newDestinationX, timeWhenReach);
        SendNetworkMsg(msg);
    }

    public void VoterClicked(VoterBehaviour voter)
    {
        localPlayer.GetComponent<PlayerBehaviour>().ChaseVoter(voter);
    }

    public void ProjectileFired(int playerOwner, Vector3 destination, float timeToReach, int projectileType)
    {
        // [(6), (player: int), (destinationVector: [x, y: floats]), (timeWhenReach: long), (projectileType: int)]
        var destinationMsg = vector2msg(destination);
        var timeWhenReach = timeToReach2timeWhenReach(timeToReach);
        var msg = string.Format(
            "[{0}, {1}, {2}, {3}, {4}]",
            (int)Codes.projectileFired,
            playerNumber,
            destinationMsg,
            timeWhenReach,
            projectileType);
        SendNetworkMsg(msg);

        var projectile = projectilesMap[projectileType];
        projectile.GetComponentInChildren<ButtonProjectileBehaviour>().OnYourProjectileFired();
        ProjectileSelected(defaultProjectile);
    }

    public void TryConvertVoter(int playerOwner, int voterId)
    {
        var time = timeToReach2timeWhenReach(0);
        var msg = string.Format(
            "[{0}, {1}, {2}, {3}]",
            (int)Codes.tryConvertVoter,
            voterId,
            playerOwner,
            time);
        SendNetworkMsg(msg);
    }

    internal void TryClaimVoter(int voterId)
    {
        var msg = string.Format(
            "[{0}, {1}]",
            (int)Codes.tryClaimVoter,
            voterId);
        SendNetworkMsg(msg);
    }

    internal event Action<GameObject> OnProjectileSelected;
    internal void ProjectileSelected(GameObject projectile)
    {
        localPlayer.GetComponent<PlayerBehaviour>().ChangeProjectile(projectile);
        OnProjectileSelected?.Invoke(projectile);
    }

    internal void TimerOver()
    {
        Debug.Log("TimerOver!");
    }

    #endregion

    private long unixMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long timeToReach2timeWhenReach(float timeToReach)
    {
        var timeToReachMillis = (int)Mathf.Round(timeToReach * 1000);
        return unixMillis() + timeToReachMillis + serverDelta;
    }

    private float timeWhenReach2timeToReach(long timeWhenReach)
    {
        return (timeWhenReach - unixMillis() - serverDelta) / 1000f;
    }

    private string vector2msg(Vector3 vector)
    {
        return string.Format("[{0}, {1}]", vector.x, vector.y);
    }

    private void SendNetworkMsg(string msg)
    {
        client.Send(Encoding.ASCII.GetBytes(msg + "\n"));
    }

    void OnApplicationQuit()
    {
        // the client/server threads won't receive the OnQuit info if we are
        // running them in the Editor. they would only quit when we press Play
        // again later. this is fine, but let's shut them down here for consistency
        client.Disconnect();
    }
}
