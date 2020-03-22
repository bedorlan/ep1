using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VoterBehaviour : MonoBehaviour
{
    const char WOMAN_CODE = '\ue900';

    private int voterId;
    private int playerOwner = -1;

    void Start()
    {
        if (UnityEngine.Random.value <= 0.5093)
        {
            GetComponent<TextMeshPro>().text = WOMAN_CODE.ToString();
        }
    }

    public void SetId(int voterId)
    {
        this.voterId = voterId;
    }

    private void OnMouseDown()
    {
        NetworkManager.singleton.VoterClicked(this);
    }

    public void RequestConvertTo(int playerOwner, bool isLocal)
    {
        if (this.playerOwner == playerOwner) return;

        GetComponent<TextMeshPro>().color = Color.gray;
        GetComponent<Jumper>().LastJump();

        if (!isLocal) return;
        NetworkManager.singleton.RequestConvertVoter(playerOwner, voterId);
    }

    public void ConvertTo(int player)
    {
        this.playerOwner = player;
        GetComponent<TextMeshPro>().color = Common.playerColors[playerOwner];
        GetComponent<Jumper>().LastJump();
    }
}
