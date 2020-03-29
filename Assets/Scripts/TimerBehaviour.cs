using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerBehaviour : MonoBehaviour
{
    public int matchTime = 120;

    internal static TimerBehaviour singleton;
    private TextMeshProUGUI textComponent;
    private int timeLeft;

    void Start()
    {
        singleton = this;
        textComponent = GetComponent<TextMeshProUGUI>();

        timeLeft = matchTime;
        SetText();
    }

    internal void StartTimer()
    {
        StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        while (timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            --timeLeft;
            SetText();
        }

        NetworkManager.singleton.TimerOver();
    }

    void SetText()
    {
        var minutes = timeLeft / 60;
        var seconds = (timeLeft % 60).ToString().PadLeft(2, '0');
        var text = string.Format("{0} {1}:{2}", Common.CLOCK_CODE, minutes, seconds);
        textComponent.text = text;
    }

    internal int GetTimeLeft()
    {
        return timeLeft;
    }
}
