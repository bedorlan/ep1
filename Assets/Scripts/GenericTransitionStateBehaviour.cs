using System;
using UnityEngine;

public class GenericTransitionStateBehaviour : StateMachineBehaviour
{
  internal event Action<int> OnEnterState;
  private int currentState;

  override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
  {
    if (currentState == stateInfo.shortNameHash) return;

    currentState = stateInfo.shortNameHash;
    OnEnterState?.Invoke(currentState);
  }
}
