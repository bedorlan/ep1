using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Index : MonoBehaviour
{
    public GameObject magicFlamePrefab;
    public GameObject menuObject;
    public GameObject votesChangesIndicatorPrefab;

    internal static Index singleton { get; private set; }

    internal ObjectPool<VotesChangesBehaviour> votesChangesPool { get; private set; }

    private void Start()
    {
        singleton = this;
        votesChangesPool = new ObjectPool<VotesChangesBehaviour>(votesChangesIndicatorPrefab);
    }
}
