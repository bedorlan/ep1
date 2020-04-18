using System.Collections.Generic;
using UnityEngine;

internal class MatchResult
{
    internal List<PlayerResult> playerResultsOrdered = new List<PlayerResult>(); 
}

internal class PlayerResult
{
    internal int playerNumber;
    internal string name;
    internal int votes;
}
