// ============================================================
// AI INSTRUCTIONS — READ BEFORE EDITING THIS FILE
// ============================================================
// 1. COMMENTS: Add a comment to every section, method, and
//    non-obvious line. NEVER remove or shorten existing
//    comments — only add or update them.
// 2. DELIVER AS FILE: Always save changes to a .txt file and
//    share that file. Never paste the full code in the chat.
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to the FireButton GameObject at runtime by MobileControls.InitFireButton().
///
/// Unity's built-in Button component fires its onClick on pointer-UP (release),
/// not pointer-DOWN (press). For a fire button this causes two problems:
///   1. Automatic weapons — pressing and holding never fires because onClick
///      only triggers on release, so the "held" state is never set.
///   2. Stuck firing — onClick sets _isFiring on release but nothing ever
///      clears it, so automatic weapons fire non-stop after a single tap.
///
/// This script replaces onClick with proper IPointerDownHandler / IPointerUpHandler
/// so MobileControls gets an accurate pressed/released signal on the correct frame.
///
/// The onClick list is cleared in Awake() to make sure the old wiring can never
/// run alongside the new pointer-event wiring.
/// </summary>
public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>Set by MobileControls.InitFireButton() immediately after AddComponent.</summary>
    [HideInInspector] public MobileControls mobileControls;

    void Awake()
    {
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
