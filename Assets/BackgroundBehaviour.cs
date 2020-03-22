using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundBehaviour : MonoBehaviour
{
    private void OnMouseDown()
    {
        var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        NetworkManager.singleton.BackgroundClicked(position);
    }
}
