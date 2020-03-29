using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundBehaviour : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (IsPointerOverUIObject()) return;

        var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        NetworkManager.singleton.BackgroundClicked(position);
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        foreach (var result in results)
        {
            if (result.gameObject.transform.root.gameObject.CompareTag("UI"))
            {
                return true;
            }
        }
        return false;
    }
}
