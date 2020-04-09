using UnityEngine;

interface IPartySupporter
{
    bool TryConvertTo(int playerOwner, bool isLocal);
    void TryConvertAndConvertOthers(int playerOwner, bool isLocal);
}
