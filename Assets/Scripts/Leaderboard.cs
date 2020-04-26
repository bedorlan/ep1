using System.Collections.Generic;

internal class Leaderboard
{
  internal List<LeaderboardItem> items;
}

internal class LeaderboardItem
{
  internal string name;
  internal int score;

  override public string ToString()
  {
    return string.Format("LeaderboardItem:{0},{1}", name, score);
  }
}
