using UnityEngine;

interface ICollectable
{
    void TryClaim(int playerNumber, bool force);
}
