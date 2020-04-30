using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class GenericTransitionStateBehaviour : StateMachineBehaviour
{
  internal event Action<string> OnEnterState;
  private Dictionary<int, string> nameMap;
  private string currentState;

  override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
  {
    if (nameMap == null) InitializeNameMap(animator);

    var newStateName = nameMap[stateInfo.shortNameHash];
    if (currentState == newStateName) return;

    currentState = newStateName;
    OnEnterState?.Invoke(currentState);
  }

  private void InitializeNameMap(Animator animator)
  {
    nameMap = new Dictionary<int, string>();
    var controller = (AnimatorController)animator.runtimeAnimatorController;
    foreach (var state in controller.layers[0].stateMachine.states)
    {
      nameMap[state.state.nameHash] = state.state.name;
    }
  }
}
