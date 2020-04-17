using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class MatchResultBehaviour : MonoBehaviour
{
    public GameObject resultsObject;    

    internal event Action OnFinished;

    internal void ShowMatchResult(MatchResult matchResult)
    {
        var playerResultsOrdered = matchResult.playerResultsOrdered;
        var playersCount = playerResultsOrdered.Count;

        for (var i = 0; i < resultsObject.transform.childCount; ++i)
        {
            var active = i < playersCount;
            var playerNameUI = resultsObject.transform.GetChild(i * 2).gameObject;
            var resultUI = resultsObject.transform.GetChild(i * 2 + 1).gameObject;
            playerNameUI.SetActive(active);
            resultUI.SetActive(active);
            if (!active) continue;

            var playerResult = playerResultsOrdered[i];
            var playerColor = Common.playerColors[playerResult.playerNumber];
            var nameText = playerNameUI.GetComponent<Text>();
            nameText.text = string.Format("Player {0}", i + 1);
            nameText.color = playerColor;

            var votesTmPro = resultUI.GetComponent<TextMeshProUGUI>();
            votesTmPro.text = string.Format("{0} {1}<size=40%>,000</size>", Common.MAN_CODE, playerResult.votes);
            votesTmPro.color = playerColor;
        }
    }

    public void OnContine()
    {
        OnFinished?.Invoke();
    }
}