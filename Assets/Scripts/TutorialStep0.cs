using System;
using System.Collections;
using UnityEngine;

public class TutorialStep0 : MonoBehaviour, ITutorialStep
{
  public event Action OnStepEnded;

  void Start()
  {
    StartCoroutine(Step());
  }

  private IEnumerator Step()
  {
    yield return new WaitForSeconds(2f);
    OnStepEnded?.Invoke();
  }
}
