using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to the "FireButton" UI button by MobileControls.FindFireButton().
///
/// Jobs:
///   1. Clear the Inspector On Click () list (prevents OnFireButtonDown being
///      called on RELEASE and leaving _isFiring stuck true).
///   2. Call OnFireButtonUp() on pointer-up so the fallback _isFiring flag
///      is always cleared when the finger lifts (handles the startup window
///      before direct touch-rect detection is ready).
/// </summary>
public class FireButton : MonoBehaviour, IPointerUpHandler
{
    [HideInInspector] public MobileControls mobileControls;

    void Awake()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonUp();
    }
}
