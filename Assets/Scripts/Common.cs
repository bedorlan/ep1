using System.Collections.Generic;
using UnityEngine;

public class Common
{
    public const char MAN_CODE = '\uf1bb';
    public const char WOMAN_CODE = '\ue901';
    public const char CLOCK_CODE = '\ue900';

    internal const int NO_PLAYER = -1;

    public static readonly Dictionary<int, Color> playerColors = new Dictionary<int, Color> {
        {NO_PLAYER, Color.gray },
        {0, new Color(1, .5f, .5f) },
        {1, new Color(.5f, .5f, 1) },
        {2, new Color(.5f, 1, .5f) },
        {3, new Color(1, 1, 0) },
    };
}
