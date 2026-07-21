using UnityEngine;

/// <summary>
/// Attach to (or auto-injected onto) the "FireButton" UI button.
///
/// ONLY JOB: clear the Button's On Click () list at runtime so the
/// Inspector-wired MobileControls.OnFireButtonDown entry can never fire
/// on release and leave _isFiring stuck true.
///
/// All fire detection is now handled by MobileControls via Input.touches +
/// RectTransformUtility — no EventSystem pointer events needed here.
/// </summary>
public class FireButton : MonoBehaviour
{
    void Awake()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
    }
}
