using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to the "FireButton" UI button by MobileControls.FindFireButton().
///
/// Jobs:
///   1. Clear the Inspector On Click () list so it never calls OnFireButtonDown
///      on RELEASE (Unity's onClick fires on pointer-up, not pointer-down).
///   2. Call OnFireButtonDown() the instant the finger TOUCHES the button
///      (IPointerDownHandler) — fixes the stuck-firing bug caused by the old
///      onClick path firing on release.
///   3. Call OnFireButtonUp() when the finger lifts so _isFiring is always
///      cleared (IPointerUpHandler).
/// </summary>
public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public MobileControls mobileControls;

    void Awake()
    {
        // Remove the Inspector On Click () entry so it can never leave
        // _isFiring stuck true by firing on pointer-up.
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
    }

    /// <summary>Finger touched the button — begin firing.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonDown();
    }

    /// <summary>Finger lifted off the button — stop firing.</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonUp();
    }
}
