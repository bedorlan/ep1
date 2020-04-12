using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnTransitionStateExitDetector : StateMachineBehaviour
{
    internal event Action OnExit;
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnExit?.Invoke();
    }
}
