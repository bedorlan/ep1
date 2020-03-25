using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CooldownBehaviour : MonoBehaviour
{
    private TextMeshProUGUI textUI;

    void Start()
    {
        textUI = GetComponent<TextMeshProUGUI>();
    }

    internal IEnumerator StartCooldown(int cooldown)
    {
        if (cooldown == 0) yield break;

        textUI.enabled = true;
        while (cooldown > 0)
        {
            textUI.text = cooldown.ToString();
            yield return new WaitForSeconds(1);
            --cooldown;
        }
        textUI.enabled = false;
    }
}
