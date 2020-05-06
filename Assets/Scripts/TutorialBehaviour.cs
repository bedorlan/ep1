using System;
using System.Collections;
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

  void Start()
  {
    StartCoroutine(TutorialRoutine());
  }

  private IEnumerator TutorialRoutine()
  {
    yield return new WaitForSeconds(2f);

    for (var i = 0; i < transform.childCount; ++i)
    {
      var step = transform.GetChild(i).gameObject;
      step.SetActive(true);

      var stepEnded = false;
      step.GetComponent<ITutorialStep>().OnStepEnded += () => stepEnded = true;
      yield return new WaitUntil(() => stepEnded);

      step.SetActive(false);
    }

    OnTutorialEnded?.Invoke();
  }
}
