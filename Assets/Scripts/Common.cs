using System.Collections.Generic;
using UnityEngine;

public class Common
{
    public static readonly Dictionary<int, Color> playerColors = new Dictionary<int, Color> {
        {0, new Color(1, .5f, .5f) },
        {1, new Color(.5f, .5f, 1) },
        {2, new Color(.5f, 1, .5f) },
        {3, new Color(1, 1, 0) },
    };
}
