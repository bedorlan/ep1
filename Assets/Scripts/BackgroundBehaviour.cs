using UnityEngine;

public class BackgroundBehaviour : MonoBehaviour
{
  private long mouseDownTime;

  private void OnMouseDown()
  {
    mouseDownTime = Common.unixMillis();
    if (Common.IsPointerOverUIObject()) return;
    if (Camera.main == null || NetworkManager.singleton == null) return;

    var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    NetworkManager.singleton.BackgroundClicked(position);
  }

  private void OnMouseDrag()
  {
    if (Common.unixMillis() - mouseDownTime < 500) return;
    OnMouseDown();
  }
}
