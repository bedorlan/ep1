using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VoterBehaviour : MonoBehaviour, IPartySupporter, ICollectable
{
    public AudioClip whenClaimedClip;

    private int voterId;
    private int playerOwner = Common.NO_PLAYER;
    private bool indifferent = false;

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
        if (Common.IsPointerOverUIObject()) return;
        NetworkManager.singleton.ObjectiveClicked(gameObject);
    }

    public bool TryConvertTo(int playerOwner, bool isLocal)
    {
        if (this.playerOwner == playerOwner || indifferent) return false;

        GetComponent<TextMeshPro>().color = Color.gray;
        GetComponent<Jumper>().LastJump();

        if (isLocal)
        {
            NetworkManager.singleton.TryConvertVoter(playerOwner, voterId);
        }
        return true;
    }

    public void TryConvertAndConvertOthers(int playerOwner, bool isLocal)
    {
        var success = TryConvertTo(playerOwner, isLocal);
        if (!success) return;

        StartCoroutine(ConvertOthers(playerOwner, isLocal));
    }

    private IEnumerator ConvertOthers(int playerOwner, bool isLocal)
    {
        yield return new WaitForSeconds(.5f);

        var collider = GetComponent<CircleCollider2D>();
        var others = new Collider2D[10];
        var count = collider.OverlapCollider(new ContactFilter2D().NoFilter(), others);

        for (var i = 0; i < count; ++i)
        {
            var voter = others[i].GetComponent<IPartySupporter>();
            if (voter == null) continue;

            voter.TryConvertAndConvertOthers(playerOwner, isLocal);
        }
    }

    public void ConvertTo(int player)
    {
        playerOwner = player;
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

    private IEnumerator die()
    {
        enabled = false;

        GetComponent<AudioSource>().PlayOneShot(whenClaimedClip);
        yield return new WaitForSeconds(whenClaimedClip.length);

        Destroy(transform.root.gameObject);
    }

    internal void BeIndifferent()
    {
        indifferent = true;
        playerOwner = Common.NO_PLAYER;
        GetComponent<TextMeshPro>().color = Common.playerColors[playerOwner];

        NetworkManager.singleton.TryConvertVoter(playerOwner, voterId);
    }

    internal void StopBeingIndifferent()
    {
        indifferent = false;
        GetComponent<TextMeshPro>().color = Color.white;
    }
}
