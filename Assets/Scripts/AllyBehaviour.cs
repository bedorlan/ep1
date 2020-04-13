using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AllyBehaviour : MonoBehaviour, IPartySupporter, ICollectable
{
    private int playerNumber;
    private Common.Projectiles projectileType;
    private bool converted = false;
    Dictionary<Common.Projectiles, char> mapProjectileIcon = new Dictionary<Common.Projectiles, char>() {
        { Common.Projectiles.Orange, Common.ARTIST_CODE },
        { Common.Projectiles.Twitter, Common.MAN_DISTRACTED_WITH_PHONE_CODE },
        { Common.Projectiles.Book, Common.MAN_READING_BOOK_CODE },
        { Common.Projectiles.Lechona, Common.CHEF_CODE },
        { Common.Projectiles.PlazaBoss, Common.CAPITALIST_CODE },
        { Common.Projectiles.Billboard, Common.SELLER_CODE },
        { Common.Projectiles.Avocado, Common.AVOCADO_GIRL_CODE },
        { Common.Projectiles.Abstention, Common.PEOPLE_CODE },
        { Common.Projectiles.Gavel, Common.JUDGE_CODE },
    };

    private void Start()
    {
        GetComponent<Renderer>().sortingLayerName = "Allies";
    }

    internal void Initialize(int playerNumber, Common.Projectiles projectileType)
    {
        this.playerNumber = playerNumber;
        this.projectileType = projectileType;

        var icon = mapProjectileIcon[projectileType];
        GetComponent<TextMeshPro>().text = icon.ToString();

        var positionX = (Random.value - 0.5f) * Common.MAP_WIDTH;
        var position = transform.position;
        position.x = positionX;
        transform.position = position;
    }

    private void OnMouseDown()
    {
        if (Common.IsPointerOverUIObject()) return;
        NetworkManager.singleton.ObjectiveClicked(gameObject);
    }

    public bool TryConvertTo(int playerOwner, bool isLocal)
    {
        if (playerOwner != playerNumber) return false;

        GetComponent<TextMeshPro>().color = Common.playerColors[playerOwner];
        converted = true;
        return true;
    }

    public void TryClaim(int playerNumber, bool force)
    {
        if (!(converted || force)) return;

        NetworkManager.singleton.NewAlly(projectileType);
        GetComponent<AudioSource>().Play();

        GetComponent<Collider2D>().enabled = false;
        GetComponent<Renderer>().enabled = false;

        Destroy(gameObject, Common.NEW_ALLY_CLIP_DURATION);
    }

    public void TryConvertAndConvertOthers(int playerOwner, bool isLocal)
    {
        TryConvertTo(playerOwner, isLocal);
    }
}
