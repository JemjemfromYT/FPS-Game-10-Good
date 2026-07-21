using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to your Fire UI button GameObject (alongside the Image/Button).
///
/// WHY THIS EXISTS:
/// Unity's standard Button.OnClick fires once on RELEASE, not on press.
/// For auto-fire weapons MobileControls needs to know exactly when the finger
/// goes DOWN (start firing) and when it comes UP (stop firing). Using the
/// standard OnClick event means OnFireButtonUp() is never called, leaving
/// _isFiring = true forever — causing nonstop shooting after a single tap.
///
/// FIX (nonstop firing bug — comprehensive):
///   1. Awake() clears the Button's onClick list so no Inspector entry can fire.
///   2. IsPointerDown property exposes real-time physical pointer state.
///      MobileControls reads this directly instead of trusting _isFiring, so
///      the auto-fire state can NEVER get stuck — it always matches your thumb.
///
/// SETUP (one-time):
///   Just place this script on the FireButton GameObject.
///   MobileControls.Start() auto-injects it if the button is named "FireButton".
///   You do NOT need to remove the existing On Click () entries — they are
///   cleared at runtime by this script.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Graphic))]
public class FireButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    // Filled automatically by MobileControls.Start(), or drag in manually.
    [HideInInspector] public MobileControls mobileControls;

    /// <summary>
    /// True only while a finger is physically touching this button.
    /// MobileControls.HandleAutoFire() reads this directly — it cannot get
    /// stuck true the way _isFiring could from the OnClick double-fire bug.
    /// </summary>
    public bool IsPointerDown { get; private set; }

    void Awake()
    {
        // Clear the Button component's On Click () list (both persistent/Inspector
        // entries and any runtime AddListener entries). This stops any wired-up
        // OnFireButtonDown() call from firing on RELEASE and getting _isFiring stuck.
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
    }

    // ── pointer events ────────────────────────────────────────────────────────

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

    // Finger slides off the button while still held — also stop firing.
    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerDown = false;
        mobileControls?.OnFireButtonUp();
    }
}
