using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicFlame : MonoBehaviour
{
    static internal MagicFlame createFlameBurst(GameObject player, float duration)
    {
        var gameObject = Instantiate(Index.singleton.magicFlame);
        var behaviour = gameObject.GetComponentInChildren<MagicFlame>();
        behaviour.player = player;
        behaviour.flameBurst = true;
        behaviour.flameDuration = duration;
        return behaviour;
    }

    static internal MagicFlame createFlameUribe(GameObject player)
    {
        var gameObject = Instantiate(Index.singleton.magicFlame);
        var behaviour = gameObject.GetComponentInChildren<MagicFlame>();
        behaviour.player = player;
        behaviour.uribeSprite.SetActive(true);
        return behaviour;
    }

    public GameObject uribeSprite;

    private GameObject player;
    private Animator animator;
    private bool flameBurst = false;
    private float flameDuration;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (flameBurst)
        {
            StartCoroutine(ShutdownDelay(flameDuration));
        }

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

    private IEnumerator ShutdownDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        Shutdown();
    }

    internal void Shutdown()
    {
        animator.SetBool("end", true);
    }

    private void MagicFlame_OnExit()
    {
        Destroy(transform.root.gameObject);
    }
}
