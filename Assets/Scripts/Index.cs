using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Index : MonoBehaviour
{
  public GameObject uiObject;
  public GameObject magicFlamePrefab;
  public GameObject menuObject;
  public GameObject votesChangesIndicatorPrefab;
  public ObjectPool voterPool;
  public ObjectPool votesChangesIndicatorPool;

  internal static Index singleton { get; private set; }

  private void Awake()
  {
    singleton = this;
  }
}
