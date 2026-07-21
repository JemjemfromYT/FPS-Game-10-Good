using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to your Fire UI button GameObject (alongside the Image/Button).
/// 
/// WHY THIS EXISTS:
/// Unity's standard Button.OnClick fires once on RELEASE, not on press.
/// For auto-fire weapons MobileControls needs to know exactly when the finger
/// goes DOWN (start firing) and when it comes UP (stop firing).  Using the
/// standard OnClick event means OnFireButtonUp() is never called, leaving
/// _isFiring = true forever — causing nonstop shooting after a single tap.
///
/// SETUP (one-time):
///   1. Select your FireButton GameObject in the Hierarchy.
///   2. Add Component → FireButton.
///   3. Drag your MobileManager (the object with MobileControls on it) into
///      the "Mobile Controls" slot.
///   4. Remove or leave the existing Button.OnClick — it is no longer needed
///      for firing; FireButton.cs takes over completely.
///
/// MobileControls.Start() also auto-injects this component onto any GameObject
/// named "FireButton" in the scene, so step 1-3 are optional if your button
/// is already named exactly "FireButton".
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Graphic))]
public class FireButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    // Filled automatically by MobileControls.Start() — or drag it in manually.
    [HideInInspector] public MobileControls mobileControls;

    // Finger DOWN → start firing
    public void OnPointerDown(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonDown();
    }

    // Finger UP → stop firing
    public void OnPointerUp(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonUp();
    }

    // Finger SLIDES OFF the button while still held → also stop firing so the
    // weapon doesn't keep shooting after the player moves their thumb away.
    public void OnPointerExit(PointerEventData eventData)
    {
        mobileControls?.OnFireButtonUp();
    }
}
