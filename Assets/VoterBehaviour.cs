using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VoterBehaviour : MonoBehaviour
{
    const char WOMAN_CODE = '\ue900';

    private int voterId;

    void Start()
    {
        if (UnityEngine.Random.value <= 0.5093)
        {
            GetComponent<TextMeshPro>().text = WOMAN_CODE.ToString();
        }
    }

    private void OnMouseDown()
    {
        NetworkManager.singleton.VoterClicked(this);
    }

    public void SetId(int voterId)
    {
        this.voterId = voterId;
    }
}
