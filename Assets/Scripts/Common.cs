using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class Common
{
    internal const int MAP_WIDTH = 200;

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

    public enum Parties
    {
        Default = 0,
        CentroDemocratico = 1,
        ColombiaHumana = 2,
        CompromisoCiudadano = 3,
    }

    public enum Projectiles
    {
        Tamal = 0,
        Lechona = 1,
        Billboard = 2,
        CentroDemocraticoBase = 4,
        ColombiaHumanaBase = 5,
        CompromisoCiudadanoBase = 6,
        Orange = 7,
    }

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
