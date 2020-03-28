using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VotesCountBehaviour : MonoBehaviour
{
    public int playerNumber = 0;

    const char MAN_CODE = '\uf1bb';
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
        var text = string.Format("{0} {1}", MAN_CODE, votes.ToString());
        textComponent.text = text;
    }

    internal void PlusOneVote()
    {
        SetVotes(++votes);
    }
}
