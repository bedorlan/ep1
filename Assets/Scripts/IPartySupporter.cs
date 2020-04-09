using UnityEngine;

interface IPartySupporter
{
    bool TryConvertTo(int playerOwner, bool isLocal);
}
