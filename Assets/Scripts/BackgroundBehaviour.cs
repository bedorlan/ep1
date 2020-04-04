using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundBehaviour : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (Common.IsPointerOverUIObject()) return;
        var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        NetworkManager.singleton.BackgroundClicked(position);
    }
}
