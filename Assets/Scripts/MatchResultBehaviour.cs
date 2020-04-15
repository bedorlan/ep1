using System;
using TMPro;
using UnityEngine;

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
            var resultUI = resultsObject.transform.GetChild(i).gameObject;
            resultUI.SetActive(active);
            if (!active) continue;

            var playerResult = playerResultsOrdered[i];
            var tmPro = resultUI.GetComponent<TextMeshProUGUI>();
            tmPro.text = string.Format("{0}{1} {2}", Common.MAN_CODE, Common.WOMAN_CODE, playerResult.votes);
            tmPro.color = Common.playerColors[playerResult.playerNumber];
        }
    }

    public void OnContine()
    {
        OnFinished?.Invoke();
    }
}