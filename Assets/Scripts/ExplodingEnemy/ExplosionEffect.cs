using System.Collections;
using UnityEngine;

// ============================================================
//  ExplosionEffect.cs
//
//  Creates a full explosion visual effect entirely in code.
//  No prefab or art assets needed.
//
//  HOW TO USE:
//    In ExplodingEnemy.cs the Explode() method already calls:
//      ExplosionEffect.Spawn(transform.position, explosionRadius);
//    Just add THIS script to your project — nothing else needed.
// ============================================================

public class ExplosionEffect : MonoBehaviour
{
    // Call this from ExplodingEnemy instead of using a prefab
    public static void Spawn(Vector3 position, float radius)
    {
        GameObject fx = new GameObject("ExplosionFX");
        fx.transform.position = position;
        ExplosionEffect effect = fx.AddComponent<ExplosionEffect>();
        effect._radius = radius;
    }

    private float _radius = 4f;

    void Start()
    {
        StartCoroutine(PlayEffect());
    }

    IEnumerator PlayEffect()
    {
        // ── 1. Flash light ────────────────────────────────────────
        GameObject lightObj = new GameObject("ExplosionLight");
        lightObj.transform.position = transform.position;
        Light flash = lightObj.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.5f, 0.1f);   // orange
        flash.intensity = 8f;
        flash.range = _radius * 3f;

        // ── 2. Shockwave ring ─────────────────────────────────────
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ring.transform.position = transform.position;
        ring.transform.localScale = Vector3.one * 0.1f;
        Destroy(ring.GetComponent<Collider>());         // no physics on the vfx sphere

        // Give the ring a transparent orange-red material
        Material ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (ringMat.shader.name == "Hidden/InternalErrorShader")
            ringMat = new Material(Shader.Find("Standard"));   // fallback for non-URP

        ringMat.color = new Color(1f, 0.35f, 0f, 0.45f);
        SetMaterialTransparent(ringMat);
        ring.GetComponent<Renderer>().material = ringMat;

        // ── 3. Debris spheres ─────────────────────────────────────
        int debrisCount = 12;
        GameObject[] debris = new GameObject[debrisCount];
        Vector3[] debrisVel = new Vector3[debrisCount];
        Material debrisMat = new Material(ringMat);
        debrisMat.color = new Color(1f, 0.6f, 0.05f, 1f);  // bright orange

        for (int i = 0; i < debrisCount; i++)
        {
            debris[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debris[i].transform.position = transform.position;
            debris[i].transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
            Destroy(debris[i].GetComponent<Collider>());
            debris[i].GetComponent<Renderer>().material = debrisMat;

            // Random outward direction
            debrisVel[i] = Random.onUnitSphere * Random.Range(_radius * 0.6f, _radius * 1.1f);
        }

        // ── Animate over time ─────────────────────────────────────
        float duration = 0.55f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;   // 0 → 1

            // Expand and fade the ring
            float ringScale = Mathf.Lerp(0.1f, _radius * 2.2f, t);
            ring.transform.localScale = Vector3.one * ringScale;
            Color rc = ringMat.color;
            rc.a = Mathf.Lerp(0.5f, 0f, t);
            ringMat.color = rc;

            // Fade and move debris
            for (int i = 0; i < debrisCount; i++)
            {
                if (debris[i] == null) continue;
                debris[i].transform.position += debrisVel[i] * Time.deltaTime;
                debrisVel[i] += Vector3.down * 9.8f * Time.deltaTime; // gravity
                Color dc = debrisMat.color;
                dc.a = Mathf.Lerp(1f, 0f, t);
                // Individual debris go from orange → red → dark
                debris[i].GetComponent<Renderer>().material.color =
                    new Color(Mathf.Lerp(1f, 0.2f, t), Mathf.Lerp(0.6f, 0f, t), 0f, dc.a);
            }

            // Fade the flash light
            flash.intensity = Mathf.Lerp(8f, 0f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ── Clean up ──────────────────────────────────────────────
        Destroy(lightObj);
        Destroy(ring);
        foreach (var d in debris)
            if (d != null) Destroy(d);
        Destroy(gameObject);
    }

    // Makes a material render transparently (works for both URP and Standard)
    static void SetMaterialTransparent(Material mat)
    {
        // URP transparent
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        // Standard shader transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }
}
