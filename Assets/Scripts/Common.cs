using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

internal static class Common
{
    internal const char MAN_CODE = '\uf1bb';
    internal const char WOMAN_CODE = '\ue901';
    internal const char CLOCK_CODE = '\ue900';

    internal const int NO_PLAYER = -1;

    internal static readonly Dictionary<int, Color> playerColors = new Dictionary<int, Color> {
        {NO_PLAYER, Color.gray },
        {0, new Color(1, .5f, .5f) },
        {1, new Color(.5f, .5f, 1) },
        {2, new Color(.5f, 1, .5f) },
        {3, new Color(1, 1, 0) },
    };

    internal static bool IsPointerOverUIObject()
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
