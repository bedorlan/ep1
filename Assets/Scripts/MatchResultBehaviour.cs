using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class MatchResultBehaviour : MonoBehaviour
{
  public GameObject resultsObject;
  public GameObject continueButton;

  private bool stop;

  internal void ShowMatchResult(MatchResult matchResult)
  {
    stop = false;
    StartCoroutine(EnableContinue());
    StartCoroutine(ShowMatchResultRoutine(matchResult));
  }

  private IEnumerator ShowMatchResultRoutine(MatchResult matchResult)
  {
    for (var it = GetPlayerFields(matchResult); it.MoveNext();)
    {
      var (playerNameUI, resultUI, playerResult) = it.Current;

      playerNameUI.SetActive(true);
      resultUI.SetActive(true);

      var playerColor = Common.playerColors[playerResult.playerNumber];
      var nameText = playerNameUI.GetComponent<Text>();
      var playerName = playerResult.name != string.Empty ? playerResult.name : string.Format("Player {0}", playerResult.playerNumber + 1);
      nameText.text = playerName;
      nameText.color = playerColor;
    }

    while (true)
    {
      for (var it = GetPlayerFields(matchResult); it.MoveNext();)
      {
        var (playerNameUI, resultUI, playerResult) = it.Current;
        var playerColor = Common.playerColors[playerResult.playerNumber];
        var votesTmPro = resultUI.GetComponent<TextMeshProUGUI>();
        votesTmPro.text = string.Format("{0} {1}<size=40%>,000</size>", Common.MAN_CODE, playerResult.votes);
        votesTmPro.color = playerColor;
      }
      yield return new WaitForSeconds(2f);
      if (stop) break;

      for (var it = GetPlayerFields(matchResult); it.MoveNext();)
      {
        var (playerNameUI, resultUI, playerResult) = it.Current;
        var votesTmPro = resultUI.GetComponent<TextMeshProUGUI>();
        var scoreDiffSign = playerResult.scoreDiff >= 0 ? "+" : "";
        votesTmPro.text = string.Format("{0} <size=60%>({1}{2})</size>", playerResult.score, scoreDiffSign, playerResult.scoreDiff);
        votesTmPro.color = Color.white;
      }
      yield return new WaitForSeconds(2f);
      if (stop) break;
    }
  }

  private IEnumerator<(GameObject, GameObject, PlayerResult)> GetPlayerFields(MatchResult matchResult)
  {
    var playerResultsOrdered = matchResult.playerResultsOrdered;
    var playersCount = playerResultsOrdered.Count;

    for (var i = 0; i < playersCount; ++i)
    {
      var active = i < playersCount;
      var playerNameUI = resultsObject.transform.GetChild(i * 2).gameObject;
      var resultUI = resultsObject.transform.GetChild(i * 2 + 1).gameObject;

      yield return (playerNameUI, resultUI, playerResultsOrdered[i]);
    }
  }

  private IEnumerator EnableContinue()
  {
    continueButton.SetActive(false);
    yield return new WaitForSeconds(3f);

    continueButton.SetActive(true);
  }

  void OnDisable()
  {
    this.stop = true;
    continueButton.SetActive(false);
    for (var i = 0; i < Common.MAX_PLAYERS_NUMBER; ++i)
    {
      resultsObject.transform.GetChild(i * 2).gameObject.SetActive(false);
      resultsObject.transform.GetChild(i * 2 + 1).gameObject.SetActive(false);
    }
  }
}