using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VoterBehaviour : MonoBehaviour
{
    public AudioClip whenClaimedClip;

    private int voterId;
    private int playerOwner = -1;

    void Start()
    {
        if (UnityEngine.Random.value <= 0.5093)
        {
            GetComponent<TextMeshPro>().text = Common.WOMAN_CODE.ToString();
        }

        GetComponent<Renderer>().sortingLayerName = "Voters";
    }

    public void SetId(int voterId)
    {
        this.voterId = voterId;
    }

    private void OnMouseDown()
    {
        NetworkManager.singleton.VoterClicked(this);
    }

    public void TryConvertTo(int playerOwner, bool isLocal)
    {
        if (this.playerOwner == playerOwner) return;

        GetComponent<TextMeshPro>().color = Color.gray;
        GetComponent<Jumper>().LastJump();

        if (!isLocal) return;
        NetworkManager.singleton.TryConvertVoter(playerOwner, voterId);
    }

    public void ConvertTo(int player)
    {
        this.playerOwner = player;
        GetComponent<TextMeshPro>().color = Common.playerColors[playerOwner];
        GetComponent<Jumper>().LastJump();
    }

    public void TryClaim(int playerNumber)
    {
        NetworkManager.singleton.TryClaimVoter(voterId);
    }

    internal void ClaimedBy(int player)
    {
        StartCoroutine(die());
    }

    internal IEnumerator die()
    {
        enabled = false;

        GetComponent<AudioSource>().PlayOneShot(whenClaimedClip);
        yield return new WaitForSeconds(whenClaimedClip.length);

        Destroy(transform.root.gameObject);
    }
}
