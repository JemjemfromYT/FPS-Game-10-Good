using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to your Fire UI button GameObject alongside the Image/Button.
///
/// WHAT THIS DOES:
///   Intercepts touch events on the fire button so MobileControls knows exactly
///   when the finger goes DOWN (start firing) and comes UP (stop firing).
///
///   Unity's standard Button.OnClick fires once on RELEASE — too late for
///   hold-to-fire weapons like the Mac10. This script replaces that with
///   IPointerDownHandler / IPointerUpHandler, which fire at the correct times.
///
/// SETUP:
///   MobileControls.Start() auto-injects this component onto any GameObject
///   named "FireButton", so you do NOT need to add it manually.
///   The existing On Click () Inspector entries are cleared at runtime — you
///   do NOT need to remove them.
///
/// NOTE — OnPointerExit is intentionally NOT implemented.
///   On mobile, the finger moving slightly off the button area fires OnPointerExit
///   even while the touch is still active. This would reset IsPointerDown to false
///   mid-hold, breaking continuous fire. OnPointerUp is always sent to the original
///   pressed object when a touch ends, so we rely on that alone to stop firing.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Graphic))]
public class FireButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    // Set automatically by MobileControls — or drag in manually.
    [HideInInspector] public MobileControls mobileControls;

    /// <summary>
    /// True while a finger is physically touching this button.
    /// Set on PointerDown, cleared on PointerUp.
    /// MobileControls.HandleAutoFire() reads this every frame — it can never
    /// get stuck the way the old _isFiring bool could.
    /// </summary>
    public bool IsPointerDown { get; private set; }

    void Awake()
    {
        // Clear the Button component's On Click () list so no Inspector entry
        // can fire OnFireButtonDown() on RELEASE and get _isFiring stuck.
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
    }

    void Start()
    {
        // Self-find MobileControls if not already set (handles cases where
        // AddComponent ran before mobileControls was assigned).
        if (mobileControls == null)
            mobileControls = Object.FindObjectOfType<MobileControls>();

        if (mobileControls != null)
            mobileControls.RegisterFireButton(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPointerDown = true;
        mobileControls?.OnFireButtonDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPointerDown = false;
        mobileControls?.OnFireButtonUp();
    }
}
