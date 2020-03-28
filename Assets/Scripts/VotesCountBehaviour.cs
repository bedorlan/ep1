using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VotesCountBehaviour : MonoBehaviour
{
    public int playerNumber = 0;

    private TextMeshProUGUI textComponent;
    private int votes = 0;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        textComponent.color = Common.playerColors[playerNumber];

        SetVotes(votes);
    }

    internal void SetVotes(int votes)
    {
        var text = string.Format("{0} {1}<size=40%>,000</size>", Common.MAN_CODE, votes.ToString());
        textComponent.text = text;
    }

    internal void PlusOneVote()
    {
        SetVotes(++votes);
    }

    internal int GetVotes()
    {
        return votes;
    }
}
