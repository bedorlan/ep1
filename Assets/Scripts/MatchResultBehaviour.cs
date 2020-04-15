using System;
using UnityEngine;

internal class MatchResultBehaviour : MonoBehaviour
{
    internal event Action OnFinished;

    internal void ShowMatchResult(MatchResult matchResult)
    {
        Debug.Log("ShowMatchResult");
    }

    public void OnContine()
    {
        OnFinished?.Invoke();
    }
}