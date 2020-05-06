using System;
using UnityEngine;

public class TutorialBehaviour : MonoBehaviour
{
  public GameObject votesCountersObject;
  public GameObject timerObject;
  public GameObject projectileButtonsObject;

  static internal TutorialBehaviour singleton { get; private set; }
  internal event Action OnTutorialEnded;

  void Awake()
  {
    singleton = this;

    for (var i = 0; i < votesCountersObject.transform.childCount; ++i)
      votesCountersObject.transform.GetChild(i).gameObject.SetActive(false);

    timerObject.SetActive(false);

    projectileButtonsObject.transform.parent.parent.gameObject.SetActive(false);
    for (var i = 0; i < projectileButtonsObject.transform.childCount; ++i)
      projectileButtonsObject.transform.GetChild(i).gameObject.SetActive(false);
  }

  public void ExitTutorial()
  {
    OnTutorialEnded?.Invoke();
  }
}
