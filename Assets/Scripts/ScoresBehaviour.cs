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
    ClearPanel(allScoresPanel);
    ClearPanel(friendsScoresPanel);

    titleObject.GetComponent<Text>().text = "<b>Cargando ...</b>";
  }

  private void ClearPanel(GameObject panel)
  {
    for (var i = 0; i < panel.transform.childCount; ++i)
    {
      panel.transform.GetChild(i).gameObject.SetActive(false);
    }
  }

  internal void ShowLeaderboardAll((Leaderboard, Leaderboard) leaderboards)
  {
    titleObject.GetComponent<Text>().text = "<b>Puntajes</b>";

    FillLeaderboard(allScoresPanel, leaderboards.Item1);
    FillLeaderboard(friendsScoresPanel, leaderboards.Item2);
  }

  private void FillLeaderboard(GameObject panel, Leaderboard leaderboard)
  {
    var items = leaderboard.items;
    var rowsMissing = items.Count - panel.transform.childCount / 2;
    CreateMissingRows(panel, rowsMissing);

    for (var i = 0; i < items.Count; ++i)
    {
      var item = items[i];
      var nameObject = panel.transform.GetChild(i * 2).gameObject;
      var scoreObject = panel.transform.GetChild(i * 2 + 1).gameObject;
      nameObject.GetComponent<Text>().text = item.name;
      scoreObject.GetComponent<Text>().text = item.score.ToString();
      nameObject.SetActive(true);
      scoreObject.SetActive(true);
    }
  }

  private void CreateMissingRows(GameObject panel, int rowsMissing)
  {
    var playerNamePrefab = panel.transform.GetChild(0);
    var scorePrefab = panel.transform.GetChild(1);
    while (rowsMissing > 0)
    {
      Instantiate(playerNamePrefab, panel.transform);
      Instantiate(scorePrefab, panel.transform);
      --rowsMissing;
    }
  }
}
