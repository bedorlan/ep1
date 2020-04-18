using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

enum Codes
{
    noop = 0, // [0]
    start = 1, // [(1), (playerNumber: int), (totalNumberOfPlayers: int)]
    newPlayerDestination = 2, // [(2), (positionX: float), (timeWhenReach: long), (playerNumber: int)]
    newVoters = 3, // [3, ...voters: [id: int, positionX: float]]
    guessTime = 5, // to server: [5, guessedTime: int], from server: [5, deltaGuess: int]
    projectileFired = 6, // [(6), (player: int), (destinationVector: [x, y: floats]), (timeWhenReach: long), (projectileType: int), (targetPlayer?: int), (projectileId: string)]
    tryConvertVoter = 7, // [(7), (voterId: int), (player: int), (time: long)]
    voterConverted = 8, // [(8), (voterId: int), (player: int)]
    tryClaimVoter = 9, // [(9), (voterId: int)]
    voterClaimed = 10, // [(10), (voterId: int), (player: int)]
    hello = 11, // [(11)]
    tryAddVotes = 12, // [(12), (playerNumber: int), (votes: int)]
    votesAdded = 13, // [(13), (playerNumber: int), (votes: int)]
    log = 14, // [(14), (condition: string), (stackTrace: string), (type: string)]
    newAlly = 15, // [(15), (playerNumber: int), (projectileType: int)]
    destroyProjectile = 16, // [(16), (projectileId: string)]
    introduce = 17, // [(17), (playerNumber: int), (playerName: string)]
}

public class NetworkManager : MonoBehaviour
{
    const float FLOOR_LEVEL_Y = -4f;

    public static NetworkManager singleton;

    public GameObject playerPrefab;
    public GameObject voterPrefab;
    public GameObject allyPrefab;
    public GameObject myCamera;
    public GameObject background;
    public GameObject projectileButtons;
    public List<GameObject> votesCounters;

    private int playerNumber;
    private Common.Parties playerParty;
    private bool matchQuit = false;
    private bool matchOver = false;
    private Dictionary<Codes, Action<JSONNode>> codesMap;
    private Dictionary<int, GameObject> projectilesMap;
    private GameObject defaultProjectile;

    private PlayerBehaviour localPlayer;
    private List<GameObject> players = new List<GameObject>();
    private List<string> playersNames = new List<string>();


    void Awake()
    {
        singleton = this;

        // update even if window isn't focused, otherwise we don't receive.
        Application.runInBackground = true;

        Config.Load();

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
            { Codes.votesAdded, OnVotesAdded },
            { Codes.newAlly, OnNewAlly },
            { Codes.destroyProjectile, OnDestroyProjectile },
            { Codes.introduce, OnIntroduce },
        };

        projectilesMap = new Dictionary<int, GameObject>();
        for (var i = 0; i < projectileButtons.transform.childCount; ++i)
        {
            var projectilePrefab = projectileButtons.transform.GetChild(i).gameObject;
            var projectileBehaviour = projectilePrefab.GetComponentInChildren<ButtonProjectileBehaviour>();
            if (projectileBehaviour == null) continue;

            var typeId = projectileBehaviour.GetProjectileTypeId();
            projectilesMap.Add(typeId, projectilePrefab);
        }

        defaultProjectile = projectilesMap[0].GetComponentInChildren<ButtonProjectileBehaviour>().projectilePrefab;
#if !UNITY_EDITOR
        projectileButtons.transform.root.GetComponentInChildren<ScrollRect>().enabled = false;
        myCamera.SetActive(false);
#endif
    }

    private Telepathy.Client client;
    internal event Action<bool> OnConnection;
    internal event Action OnMatchReady;
    internal event Action OnMatchQuit;
    internal event Action<MatchResult> OnMatchEnd;

    internal void TryConnect()
    {
        client = new Telepathy.Client();

#if UNITY_EDITOR || !UNITY_ANDROID
        client.Connect("localhost", 7777);
#else
        client.Connect(Config.serverHost, Config.serverPort);
#endif
    }

    void Update()
    {
        Telepathy.Message msg;
        while (client != null && client.GetNextMessage(out msg))
        {
            switch (msg.eventType)
            {
                case Telepathy.EventType.Connected:
                    sayHello();
                    break;
                case Telepathy.EventType.Data:
                    ProcessRemoteMsg(msg.data);
                    break;
                case Telepathy.EventType.Disconnected:
                    OnDisconnected();
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
#if !UNITY_EDITOR
        Application.logMessageReceived += Application_logMessageReceived;
#endif

        OnConnection?.Invoke(true);
        guessServerTime();
    }

    private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (client == null || !client.Connected) return;

        var array = new JSONArray();
        array.Add(new JSONNumber((int)Codes.log));
        array.Add(new JSONString(condition));
        array.Add(new JSONString(stackTrace));
        array.Add(new JSONString(type.ToString()));

        var msg = array.ToString();
        SendNetworkMsg(msg);
    }

    private void StartGame(JSONNode data)
    {
        playerNumber = data[1].AsInt;
        var numberOfPlayers = data[2].AsInt;

        for (var i = 0; i < numberOfPlayers; ++i)
        {
            var player = Instantiate(playerPrefab);
            var isLocal = playerNumber == i;
            var playerBehaviour = player.GetComponent<PlayerBehaviour>();
            playerBehaviour.Initialize(i, isLocal);

            if (isLocal)
            {
                localPlayer = playerBehaviour;
                myCamera.GetComponent<MainCamera>().objectToFollow = player;
            }

            players.Add(player);
            playersNames.Add(string.Empty);
        }
        playersNames[playerNumber] = SocialBehaviour.singleton.shortName;

        ignoreCollisions();

        const int MAX_NUMBER_OF_PLAYERS = 4;
        for (var i = numberOfPlayers; i < MAX_NUMBER_OF_PLAYERS; ++i)
        {
            votesCounters[i].SetActive(false);
        }

        ProjectileSelected(defaultProjectile);

#if !UNITY_EDITOR
        for (var i = 1; i < projectileButtons.transform.childCount; ++i)
        {
            projectileButtons.transform.GetChild(i).gameObject.SetActive(false);
        }

        StartCoroutine(ShowPartySelection());
#endif

        OnMatchReady?.Invoke();
        myCamera.SetActive(true);
        TimerBehaviour.singleton.StartTimer();

        Introduce();
    }

    private void Introduce()
    {
        var msg = new JSONArray();
        msg.Add((int)Codes.introduce);
        msg.Add(playerNumber);
        msg.Add(SocialBehaviour.singleton.shortName);
        SendNetworkMsg(msg.ToString());
    }

    private void OnIntroduce(JSONNode data)
    {
        var remotePlayerNumber = data[1].AsInt;
        var remotePlayerName = (string)data[2];
        playersNames[remotePlayerNumber] = remotePlayerName;
    }

    private IEnumerator ShowPartySelection()
    {
        yield return new WaitForSecondsRealtime(20f);

        projectileButtons.transform.GetChild((int)Common.Projectiles.CentroDemocraticoBase).gameObject.SetActive(true);
        projectileButtons.transform.GetChild((int)Common.Projectiles.ColombiaHumanaBase).gameObject.SetActive(true);
        projectileButtons.transform.GetChild((int)Common.Projectiles.CompromisoCiudadanoBase).gameObject.SetActive(true);
    }

    internal void PartyChose(int playerNumber, Common.Parties party)
    {
        var player = players[playerNumber];
        player.GetComponent<PlayerBehaviour>().PartyChose(party);
        if (playerNumber != this.playerNumber) return;

        this.playerParty = party;
        projectileButtons.transform.GetChild((int)Common.Projectiles.CentroDemocraticoBase).gameObject.SetActive(false);
        projectileButtons.transform.GetChild((int)Common.Projectiles.ColombiaHumanaBase).gameObject.SetActive(false);
        projectileButtons.transform.GetChild((int)Common.Projectiles.CompromisoCiudadanoBase).gameObject.SetActive(false);

        StartCoroutine(StartGamePlan());
    }

    readonly Dictionary<Common.Parties, Common.Projectiles[][]> mapPartyToProjectiles = new Dictionary<Common.Parties, Common.Projectiles[][]>() {
        { Common.Parties.CentroDemocratico, new Common.Projectiles[][] {
                new Common.Projectiles[] { Common.Projectiles.Orange, Common.Projectiles.Lechona },
                new Common.Projectiles[] { Common.Projectiles.PlazaBoss, Common.Projectiles.Billboard },
                new Common.Projectiles[] { Common.Projectiles.Uribe },
            }
        },
        { Common.Parties.ColombiaHumana, new Common.Projectiles[][] {
                new Common.Projectiles[] { Common.Projectiles.Twitter, Common.Projectiles.Lechona },
                new Common.Projectiles[] { Common.Projectiles.Avocado, Common.Projectiles.Billboard },
                new Common.Projectiles[] { Common.Projectiles.Gavel },
            }
        },
        { Common.Parties.CompromisoCiudadano, new Common.Projectiles[][] {
                new Common.Projectiles[] { Common.Projectiles.Book, Common.Projectiles.Lechona },
                new Common.Projectiles[] { Common.Projectiles.Abstention, Common.Projectiles.Billboard },
                new Common.Projectiles[] { Common.Projectiles.Transparency },
            }
        },
    };

    readonly Dictionary<Common.Projectiles, List<(Common.Projectiles, AllyBehaviour)>>
        alliesInMatch = new Dictionary<Common.Projectiles, List<(Common.Projectiles, AllyBehaviour)>>();

    private IEnumerator StartGamePlan()
    {
        // 0:20 ShowPartySelection & PartyChose
        // 0:30 level 1
        // 1:15 level 2
        // 2:00 level 3
        // 3:00 clocks off
        // 3:00 debate?
        // 4:00 end match

        yield return new WaitForSecondsRealtime(10f);

        var projectileLevels = mapPartyToProjectiles[playerParty];
        for (var level = 0; level < 3; ++level)
        {
            var projectiles = projectileLevels[level];
            var newAlliesInMatch = new List<(Common.Projectiles, AllyBehaviour)>();
            foreach (var projectile in projectiles)
            {
                var ally = Instantiate(allyPrefab).GetComponent<AllyBehaviour>();
                ally.Initialize(playerNumber, projectile);

                newAlliesInMatch.Add((projectile, ally));
                alliesInMatch[projectile] = newAlliesInMatch;
            }

            yield return new WaitForSecondsRealtime(45f);
        }
    }

    private void OnRemoteNewDestination(JSONNode data)
    {
        var newDestination = data[1].AsFloat;
        var timeWhenReach = data[2].AsLong;
        var remotePlayerNumber = data[3].AsInt;
        var remotePlayer = players[remotePlayerNumber];
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

            var voterPosition = new Vector3(voterPositionX, FLOOR_LEVEL_Y + .3f, 0f);
            var newVoter = Instantiate(voterPrefab, voterPosition, Quaternion.identity);
            newVoter.GetComponent<VoterBehaviour>().SetId(voterId);

            votersMap.Add(voterId, newVoter);
        }
    }

    private void OnProjectileFired(JSONNode data)
    {
        var remotePlayerNumber = data[1].AsInt;
        var destination = new Vector3(data[2].AsArray[0].AsFloat, data[2].AsArray[1].AsFloat, 0f);
        var timeWhenReach = data[3].AsLong;
        var timeToReach = timeWhenReach2timeToReach(timeWhenReach);
        var projectileType = data[4].AsInt;
        var playerTarget = data[5].IsNull ? null : players[data[5].AsInt];
        var projectileId = (string)data[6];
        var projectile = projectilesMap[projectileType].GetComponentInChildren<ButtonProjectileBehaviour>().projectilePrefab;
        var remotePlayer = players[remotePlayerNumber];
        remotePlayer.GetComponent<PlayerBehaviour>().Remote_FireProjectile(projectile, destination, timeToReach, playerTarget, projectileId);
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

        votesCounters[player].GetComponent<VotesCountBehaviour>().AddVotes(1);
        players[player].GetComponent<PlayerBehaviour>().OnVotesChanges(1);
    }

    private void OnVotesAdded(JSONNode data)
    {
        var player = data[1].AsInt;
        var votes = data[2].AsInt;

        votesCounters[player].GetComponent<VotesCountBehaviour>().AddVotes(votes);
        players[player].GetComponent<PlayerBehaviour>().OnVotesChanges(votes);
    }

    private void OnNewAlly(JSONNode data)
    {
        var playerNumber = data[1].AsInt;
        var projectileType = (Common.Projectiles)data[2].AsInt;
        var player = players[playerNumber];

        StartCoroutine(player.GetComponent<PlayerBehaviour>().NewAlly(projectileType));
    }

    private void OnDestroyProjectile(JSONNode data)
    {
        var projectileId = (string)data[1];
        Projectile.DestroyProjectile(projectileId);
    }

    private void OnDisconnected()
    {
        var timeLeft = TimerBehaviour.singleton.GetTimeLeft();
        if (!(matchOver || matchQuit) && timeLeft > 5)
        {
            // intentar reconectar!
            OnConnection?.Invoke(false);
            return;
        }

        if (!matchQuit) TimerOver();
    }

#endregion

    private void ignoreCollisions()
    {
        foreach (var player in players)
        {
            var collider = player.GetComponent<Collider2D>();
            Physics2D.IgnoreCollision(collider, background.GetComponent<Collider2D>());
        }
    }

    private long timer = -1;
    private int minServerLatency = int.MaxValue;
    private int serverDelta = 0;

    private void guessServerTime()
    {
        var now = timer = Common.unixMillis();
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

        var newLatency = (int)(Common.unixMillis() - timer);
        timer = -1;
        if (newLatency < minServerLatency)
        {
            serverDelta = newDelta - (newLatency / 2);
            minServerLatency = newLatency;
        }

        if (minServerLatency <= 40) yield break;

        yield return new WaitForSeconds(1f);
        guessServerTime();
    }

#region local events

    public void BackgroundClicked(Vector3 position)
    {
        localPlayer.NewObjective(position, null);
    }

    internal void ObjectiveClicked(GameObject objective, Vector3? position = null)
    {
        var root = objective.transform.root;
        position = position ?? root.position;
        localPlayer.NewObjective(position.Value, root.gameObject);
    }

    public void NewLocalPlayerDestination(float newDestinationX, float timeToReach)
    {
        var timeWhenReach = timeToReach2timeWhenReach(timeToReach);
        var msg = string.Format("[{0}, {1}, {2}, {3}]",
            (int)Codes.newPlayerDestination,
            newDestinationX,
            timeWhenReach,
            playerNumber);
        SendNetworkMsg(msg);
    }

    public void ProjectileFired(
        int playerOwner,
        Vector3 destination,
        float timeToReach,
        int projectileType,
        GameObject targetObject,
        string projectileId)
    {
        var playerTarget = targetObject?.GetComponent<PlayerBehaviour>()?.GetPlayerNumber() ?? null;
        var timeWhenReach = timeToReach2timeWhenReach(timeToReach);
        var destinationMsg = new JSONArray();
        destinationMsg.Add(destination.x);
        destinationMsg.Add(destination.y);

        var msg = new JSONArray();
        msg.Add((int)Codes.projectileFired);
        msg.Add(playerOwner);
        msg.Add(destinationMsg);
        msg.Add(timeWhenReach);
        msg.Add(projectileType);
        msg.Add(playerTarget);
        msg.Add(projectileId);
        SendNetworkMsg(msg.ToString());

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
        localPlayer.ChangeProjectile(projectile);
        OnProjectileSelected?.Invoke(projectile);
    }

    internal void AddVotes(int playerNumber, int votes)
    {
        var msg = string.Format(
            "[{0}, {1}, {2}]",
            (int)Codes.tryAddVotes,
            playerNumber,
            votes);
        SendNetworkMsg(msg);
    }

    internal void NewAlly(Common.Projectiles projectileType)
    {
        projectileButtons.transform.GetChild((int)projectileType).gameObject.SetActive(true);
        foreach (var alliesAtLevel in alliesInMatch[projectileType])
        {
            if (alliesAtLevel.Item1 == projectileType) continue;
            Destroy(alliesAtLevel.Item2.transform.root.gameObject);
        }

        var msg = new JSONArray();
        msg.Add((int)Codes.newAlly);
        msg.Add(playerNumber);
        msg.Add((int)projectileType);
        SendNetworkMsg(msg.ToString());

        StartCoroutine(localPlayer.NewAlly(projectileType));
    }

    internal void DestroyProjectile(string projectileId)
    {
        var msg = new JSONArray();
        msg.Add((int)Codes.destroyProjectile);
        msg.Add(projectileId);
        SendNetworkMsg(msg.ToString());
    }

    public void OnShowMenu()
    {
        Index.singleton.menuObject.SetActive(true);
    }

    public void OnHideMenu()
    {
        Index.singleton.menuObject.SetActive(false);
    }

    public void OnExitMatch()
    {
        matchQuit = true;
        client.Disconnect();
        OnMatchQuit?.Invoke();
    }

    internal void TimerOver()
    {
        matchOver = true;
        client.Disconnect();

        // todo: votes count should come from the server
        var playerResultsOrdered = votesCounters
            .Where(voter => voter.activeSelf)
            .Select(voter => voter.GetComponent<VotesCountBehaviour>().GetVotes())
            .Select((votes, index) => new PlayerResult() {
                playerNumber = index,
                name = playersNames[index],
                votes = votes,
            })
            .OrderByDescending(a => a.votes)
            .ToList();

        var matchResult = new MatchResult() { playerResultsOrdered = playerResultsOrdered };
        OnMatchEnd?.Invoke(matchResult);
    }

#endregion

    private long timeToReach2timeWhenReach(float timeToReach)
    {
        var timeToReachMillis = (int)Mathf.Round(timeToReach * 1000);
        return Common.unixMillis() + timeToReachMillis + serverDelta;
    }

    private float timeWhenReach2timeToReach(long timeWhenReach)
    {
        return (timeWhenReach - Common.unixMillis() - serverDelta) / 1000f;
    }

    private void SendNetworkMsg(string msg)
    {
        var bytes = Encoding.ASCII.GetBytes(msg);
        client.Send(bytes);
    }

    void OnApplicationQuit()
    {
        // the client/server threads won't receive the OnQuit info if we are
        // running them in the Editor. they would only quit when we press Play
        // again later. this is fine, but let's shut them down here for consistency
        client.Disconnect();
    }
}
