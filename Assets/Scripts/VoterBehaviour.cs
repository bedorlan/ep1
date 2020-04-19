using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VoterBehaviour : MonoBehaviour, IPartySupporter, ICollectable, IPoolable
{
    public AudioClip whenClaimedClip;
    public event Action<GameObject> Despawn;

    private int voterId;
    private int playerOwner = Common.NO_PLAYER;

    void Start()
    {
        if (UnityEngine.Random.value <= 0.5093)
        {
            GetComponent<TextMeshPro>().text = Common.WOMAN_CODE.ToString();
        }

        GetComponent<Renderer>().sortingLayerName = "Voters";
        Init();
    }

    public void Spawn() { }

    private void Init()
    {
        playerOwner = Common.NO_PLAYER;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<TextMeshPro>().color = Color.white;
        GetComponent<Jumper>().StartJumpingAgain();
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
        if (!transform.root.gameObject.activeSelf) return false;
        if (this.playerOwner == playerOwner) return false;

        GetComponent<TextMeshPro>().color = Color.gray;
        var jumper = GetComponent<Jumper>();
        if (jumper != null) jumper.LastJump();

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
            var voter = others[i].GetComponentInChildren<IPartySupporter>();
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

    public void TryClaim(int playerNumber, bool force)
    {
        if (!transform.root.gameObject.activeSelf) return;
        if (!(playerOwner == playerNumber || force)) return;
        NetworkManager.singleton.TryClaimVoter(voterId);
    }

    internal void ClaimedBy(int player)
    {
        StartCoroutine(KillSelf());
    }

    private IEnumerator KillSelf()
    {
        GetComponent<Collider2D>().enabled = false;
        GetComponent<AudioSource>().PlayOneShot(whenClaimedClip);

        yield return new WaitForSeconds(whenClaimedClip.length);

        Init();
        Despawn?.Invoke(transform.root.gameObject);
    }

    internal void BeUndecided(bool isLocal)
    {
        TryConvertTo(Common.NO_PLAYER, isLocal);
    }
}
