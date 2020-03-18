using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VoterBehaviour : MonoBehaviour
{
    const char WOMAN_CODE = '\ue900';

    void Start()
    {
        if (Random.value <= 0.5093)
        {
            GetComponent<TextMeshPro>().text = WOMAN_CODE.ToString();
        }
    }

    private void OnMouseDown()
    {
        NetworkManager.singleton.VoterClicked(this);
    }
}
