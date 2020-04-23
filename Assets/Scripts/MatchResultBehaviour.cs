using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class MatchResultBehaviour : MonoBehaviour
{
  public GameObject resultsObject;
  public GameObject continueButton;

  internal event Action OnFinished;

  internal void ShowWaitingForMatchResult()
  {
    continueButton.SetActive(false);

    for (var i = 0; i < Common.MAX_PLAYERS_NUMBER; ++i)
    {
      resultsObject.transform.GetChild(i * 2).gameObject.SetActive(false);
      resultsObject.transform.GetChild(i * 2 + 1).gameObject.SetActive(false);
    }

    // todo: loading perhaps?
  }

  internal void ShowMatchResult(MatchResult matchResult)
  {
    var playerResultsOrdered = matchResult.playerResultsOrdered;
    var playersCount = playerResultsOrdered.Count;

    for (var i = 0; i < Common.MAX_PLAYERS_NUMBER; ++i)
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
      var playerName = playerResult.name != string.Empty ? playerResult.name : string.Format("Player {0}", i + 1);
      nameText.text = playerName;
      nameText.color = playerColor;

      var votesTmPro = resultUI.GetComponent<TextMeshProUGUI>();
      votesTmPro.text = string.Format("{0} {1}<size=40%>,000</size>", Common.MAN_CODE, playerResult.votes);
      votesTmPro.color = playerColor;
    }

    StartCoroutine(EnableContinue());
  }

  private IEnumerator EnableContinue()
  {
    continueButton.SetActive(false);
    yield return new WaitForSeconds(2f);

    continueButton.SetActive(true);
  }

  public void OnContine()
  {
    OnFinished?.Invoke();
  }
}