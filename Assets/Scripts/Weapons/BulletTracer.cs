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

    public void Initialize(Vector3 startPosition, Vector3 endPosition, float tracerLifetime, Action<BulletTracer> releaseCallback)
    {
        start = startPosition;
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
        float t = Mathf.Clamp01(timer / lifetime);
        Vector3 current = Vector3.Lerp(start, end, t);

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, current);

        if (timer >= lifetime)
        {
            release?.Invoke(this);
        }
    }
}