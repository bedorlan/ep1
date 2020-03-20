using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VoterBehaviour : MonoBehaviour
{
    const char WOMAN_CODE = '\ue900';

    private int voterId;
    private int playerOwner;

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

    internal void ConvertTo(int playerOwner)
    {
        this.playerOwner = playerOwner;
        GetComponent<TextMeshPro>().color = Common.playerColors[playerOwner];
    }
}
