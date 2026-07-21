using System;
using UnityEngine;

[DisallowMultipleComponent]
public class BulletTracer : MonoBehaviour
{
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] float startWidth = 0.02f;
    [SerializeField] float endWidth = 0.02f;

    Vector3 start;
    Vector3 end;
    float lifetime;
    float timer;
    Action<BulletTracer> release;

    // FIX 3 (tracer lag): track the gun barrel transform so the tracer
    // origin follows the weapon when the player is sprinting / moving fast.
    Transform originTransform;

    void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
    }

    /// <summary>
    /// Initialise the tracer.
    /// </summary>
    /// <param name="startPosition">World-space spawn position of the tracer.</param>
    /// <param name="origin">The gun-barrel Transform; if supplied, the tracer
    /// start point tracks it every frame so there is no lag at high speed.</param>
    /// <param name="endPosition">World-space hit/end point.</param>
    /// <param name="tracerLifetime">Seconds before the tracer is returned to the pool.</param>
    /// <param name="releaseCallback">Called when the tracer is done.</param>
    public void Initialize(Vector3 startPosition, Transform origin, Vector3 endPosition,
                           float tracerLifetime, Action<BulletTracer> releaseCallback)
    {
        start = startPosition;
        originTransform = origin;
        end = endPosition;
        lifetime = Mathf.Max(0.01f, tracerLifetime);
        timer = 0f;
        release = releaseCallback;

        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, start);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // If the barrel transform is still alive, keep the start position
        // glued to it — prevents the tracer from floating behind a fast mover.
        if (originTransform != null)
            start = originTransform.position;

        float t = Mathf.Clamp01(timer / lifetime);
        Vector3 current = Vector3.Lerp(start, end, t);

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, current);

        if (timer >= lifetime)
            release?.Invoke(this);
    }
}
