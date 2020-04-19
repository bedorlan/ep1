using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumper : MonoBehaviour
{
    const float JUMP_SPEED = 2f;
    const float JUMP_HIGH = .25f;
    const float WAIT_TIL_NEXT_JUMP = 3f;
    readonly float X_MAX = 2f * Mathf.Sqrt(JUMP_HIGH) + WAIT_TIL_NEXT_JUMP;

    private float groundY;
    private float x = 0f;
    private bool lastJump = false;

    void Start()
    {
        groundY = transform.position.y;
    }

    private void Update()
    {
        if (x == 0f && lastJump)
        {
            return;
        }

        x += Time.deltaTime;
        var y = groundY;
        if (x < X_MAX)
        {
            y = Mathf.Max(groundY, JumpFunction(x * JUMP_SPEED));
        }
        else if (x >= X_MAX)
        {
            x = 0f;
        }

        var newPosition = transform.position;
        newPosition.y = y;
        transform.position = newPosition;
    }

    private float JumpFunction(float x)
    {
        return JUMP_HIGH - Mathf.Pow(x - Mathf.Sqrt(JUMP_HIGH), 2) + groundY;
    }

    internal void LastJump()
    {
        lastJump = true;
    }

    internal void StartJumpingAgain()
    {
        lastJump = false;
    }
}
