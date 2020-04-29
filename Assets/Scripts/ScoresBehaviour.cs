using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoresBehaviour : MonoBehaviour
{
  public GameObject titleObject;
  public GameObject allScoresPanel;
  public GameObject friendsScoresPanel;

  internal void ShowLoading()
  {
    for (var i = 0; i < allScoresPanel.transform.childCount; ++i)
    {
      allScoresPanel.transform.GetChild(i).gameObject.SetActive(false);
    }
    for (var i = 0; i < friendsScoresPanel.transform.childCount; ++i)
    {
      friendsScoresPanel.transform.GetChild(i).gameObject.SetActive(false);
    }

    titleObject.GetComponent<Text>().text = "<b>Cargando ...</b>";
  }

  internal void ShowLeaderboardAll(Leaderboard leaderboard)
  {
    titleObject.GetComponent<Text>().text = "<b>Puntajes</b>";

    var items = leaderboard.items;
    var rowsMissing = items.Count - allScoresPanel.transform.childCount / 2;
    CreateMissingRows(rowsMissing);

    for (var i = 0; i < items.Count; ++i)
    {
      var item = items[i];
      var nameObject = allScoresPanel.transform.GetChild(i * 2).gameObject;
      var scoreObject = allScoresPanel.transform.GetChild(i * 2 + 1).gameObject;
      nameObject.GetComponent<Text>().text = item.name;
      scoreObject.GetComponent<Text>().text = item.score.ToString();
      nameObject.SetActive(true);
      scoreObject.SetActive(true);
    }
  }

  private void CreateMissingRows(int rowsMissing)
  {
    var playerNamePrefab = allScoresPanel.transform.GetChild(0);
    var scorePrefab = allScoresPanel.transform.GetChild(1);
    while (rowsMissing > 0)
    {
      Instantiate(playerNamePrefab, allScoresPanel.transform);
      Instantiate(scorePrefab, allScoresPanel.transform);
      --rowsMissing;
    }
  }
}
