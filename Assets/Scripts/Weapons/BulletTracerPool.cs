using System.Collections.Generic;
using UnityEngine;

public static class BulletTracerPool
{
    static readonly Queue<BulletTracer> pool = new Queue<BulletTracer>(64);
    static Transform root;
    static BulletTracer fallbackPrefab;

    static void EnsureRoot()
    {
        if (root != null) return;
        GameObject go = new GameObject("BulletTracerPool");
        Object.DontDestroyOnLoad(go);
        root = go.transform;
    }

    static BulletTracer GetPrefab(BulletTracer prefab)
    {
        if (prefab != null) return prefab;
        if (fallbackPrefab != null) return fallbackPrefab;

        GameObject go = new GameObject("BulletTracer_Fallback");
        go.SetActive(false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(1f, 0.95f, 0.4f, 1f);

        fallbackPrefab = go.AddComponent<BulletTracer>();
        return fallbackPrefab;
    }

    // FIX 3 (tracer lag): accepts the barrel Transform so BulletTracer can
    // track it each frame instead of using a frozen world position.
    public static void Spawn(Vector3 start, Transform origin, Vector3 end,
                             BulletTracer prefab, float lifetime)
    {
        EnsureRoot();

        BulletTracer tracer = pool.Count > 0
            ? pool.Dequeue()
            : Object.Instantiate(GetPrefab(prefab), root);

        tracer.gameObject.SetActive(true);
        tracer.Initialize(start, origin, end, lifetime, Release);
    }

    static void Release(BulletTracer tracer)
    {
        if (tracer == null) return;
        tracer.gameObject.SetActive(false);
        tracer.transform.SetParent(root, false);
        pool.Enqueue(tracer);
    }
}
