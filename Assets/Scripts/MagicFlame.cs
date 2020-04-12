using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicFlame : MonoBehaviour
{
    static internal MagicFlame createFlameBurst(GameObject player)
    {
        var gameObject = Instantiate(Index.singleton.magicFlame);
        var behaviour = gameObject.GetComponentInChildren<MagicFlame>();
        behaviour.player = player;
        behaviour.flameBurst = true;
        return behaviour;
    }

    private GameObject player;
    private Animator animator;
    private bool flameBurst = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (flameBurst) animator.SetBool("end", true);

        var playerNumber = player.GetComponent<PlayerBehaviour>().GetPlayerNumber();
        var color = Common.playerColors[playerNumber];
        GetComponent<SpriteRenderer>().color = color;

        animator.GetBehaviour<OnTransitionStateExitDetector>().OnExit += MagicFlame_OnExit;
    }

    void Update()
    {
        var position = player.transform.position;
        position.x += -0.2f;
        position.y = -0.43f;
        transform.position = position;
    }

    private void MagicFlame_OnExit()
    {
        Destroy(transform.root.gameObject);
    }
}
