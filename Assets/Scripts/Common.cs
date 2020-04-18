using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class Common
{
    internal const int MAX_PLAYERS_NUMBER = 4;
    internal const int MAP_WIDTH = 200;
    internal const float NEW_ALLY_CLIP_DURATION = 1.897f;

    internal const char SELLER_CODE = '\uf114';
    internal const char CHEF_CODE = '\uf119';
    internal const char PEOPLE_CODE = '\uf11e';
    internal const char PLAZA_BOSS_CODE = '\uf12f';
    internal const char FARMER_CODE = '\uf133';
    internal const char GIRL_DANCING_CODE = '\uf13d';
    internal const char CAPITALIST_CODE = '\uf150';
    internal const char MONK_CODE = '\uf151';
    internal const char MAN_READING_BOOK_CODE = '\uf180';
    internal const char MAN_DISTRACTED_WITH_PHONE_CODE = '\uf18e';
    internal const char SHAKESPEARE_CODE = '\uf190';
    internal const char ARTIST_CODE = '\uf1a0';
    internal const char MAN_CODE = '\uf1bb';
    internal const char AVOCADO_GIRL_CODE = '\uf1d5';
    internal const char CLOCK_CODE = '\ue900';
    internal const char WOMAN_CODE = '\ue901';
    internal const char JUDGE_CODE = '\ue902';
    internal const char MAN_SWEARING_CODE = '\ue903';

    internal const int NO_PLAYER = -1;

    internal static readonly Dictionary<int, Color> playerColors = new Dictionary<int, Color> {
        {NO_PLAYER, Color.white },
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
        CentroDemocraticoBase = 4,
        ColombiaHumanaBase = 5,
        CompromisoCiudadanoBase = 6,
        Orange = 7,
        Twitter = 8,
        Book = 9,
        PlazaBoss = 10,
        Billboard = 11,
        Avocado = 12,
        Abstention = 13,
        Gavel = 14,
        Uribe = 15,
        Transparency = 16,
    }

    static internal bool IsPointerOverUIObject()
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

    static internal long unixMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
