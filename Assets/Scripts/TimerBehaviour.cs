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
    private int timeElapsed;

    void Start()
    {
        singleton = this;
        textComponent = GetComponent<TextMeshProUGUI>();

        timeElapsed = matchTime;
        SetText();
    }

    internal void StartTimer()
    {
        StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        while (timeElapsed > 0)
        {
            yield return new WaitForSeconds(1);
            --timeElapsed;
            SetText();
        }

        NetworkManager.singleton.TimerOver();
    }

    void SetText()
    {
        var minutes = (int)(timeElapsed / 60);
        var seconds = (timeElapsed % 60).ToString().PadLeft(2, '0');
        var text = string.Format("{0} {1}:{2}", Common.CLOCK_CODE, minutes, seconds);
        textComponent.text = text;
    }

    internal int GetElapsedTime()
    {
        return timeElapsed;
    }
}
