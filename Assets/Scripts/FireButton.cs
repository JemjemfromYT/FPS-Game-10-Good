using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to the "FireButton" UI button by MobileControls.FindFireButton().
///
/// Uses IPointerDownHandler + IPointerUpHandler so the MobileControls
/// receives the EXACT moment the finger touches and lifts — no stuck flags,
/// no delayed onClick events.
/// </summary>
public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public MobileControls mobileControls;

    void Awake()
    {
        // Clear the Inspector On Click () list so it can never call
        // OnFireButtonDown on RELEASE and leave _isFiring stuck true.
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
    }

    // Called the instant the finger touches the button.
    public void OnPointerDown(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonDown();
    }

    // Called the instant the finger lifts off the button.
    public void OnPointerUp(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonUp();
    }
}
