using UnityEngine;
using UnityEngine.EventSystems;

// ============================================================
//  VirtualJoystick.cs
//
//  Attach this to the joystick BACKGROUND image in your Canvas.
//  It tracks the drag and exposes Horizontal / Vertical (-1 to 1).
//
//  HOW TO SET UP IN EDITOR:
//    1. Create a Canvas (Screen Space - Overlay)
//    2. Add an Image for the joystick background (bottom-left area)
//    3. Add a child Image for the joystick handle (knob)
//    4. Attach this script to the BACKGROUND image
//    5. Drag the knob Image into the "Handle" field in Inspector
// ============================================================

public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("The small circle that moves when you drag")]
    public RectTransform handle;

    [Tooltip("How far (in pixels) the handle can move from center")]
    public float radius = 60f;

    // Read these from your player movement script
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    private RectTransform _bg;
    private Vector2 _center;
    private int _fingerId = -1;

    void Awake()
    {
        _bg = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (_fingerId != -1) return;    // already tracking a finger
        _fingerId = e.pointerId;
        _center = e.position;
        MoveHandle(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != _fingerId) return;
        MoveHandle(e.position);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != _fingerId) return;
        _fingerId = -1;
        Horizontal = 0f;
        Vertical = 0f;
        handle.anchoredPosition = Vector2.zero;
    }

    void MoveHandle(Vector2 touchPos)
    {
        Vector2 delta = touchPos - _center;

        if (delta.magnitude > radius)
            delta = delta.normalized * radius;

        handle.anchoredPosition = delta;

        Horizontal = delta.x / radius;
        Vertical = delta.y / radius;
    }
}
