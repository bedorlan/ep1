using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AllyBehaviour : MonoBehaviour, IPartySupporter, ICollectable
{
    const char CHEF_CODE = '\uf119';

    private int playerNumber;
    private Common.Projectiles projectileType;
    private bool converted = false;
    Dictionary<Common.Projectiles, char> mapProjectileIcon = new Dictionary<Common.Projectiles, char>() {
        { Common.Projectiles.Lechona, CHEF_CODE },
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

    public void TryConvertTo(int playerOwner, bool isLocal)
    {
        if (playerOwner != playerNumber) return;

        GetComponent<TextMeshPro>().color = Common.playerColors[playerOwner];
        converted = true;
    }

    public void TryClaim(int playerNumber)
    {
        if (!converted) return;

        NetworkManager.singleton.NewAlly(projectileType);
        GetComponent<AudioSource>().Play();

        GetComponent<Collider2D>().enabled = false;
        GetComponent<Renderer>().enabled = false;

        Destroy(gameObject, 3f);
    }
}
