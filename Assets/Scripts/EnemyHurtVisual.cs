using System;
using System.Collections;
using UnityEngine;

public class EnemyHurtVisual : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] Material hurtMaterialTemplate;
    [SerializeField] float hurtFlashDuration = 0.45f;
    [SerializeField] float deathFlashDuration = 0.4f;

    Renderer targetRenderer;
    Material normalMaterial;
    Material hurtMaterial;
    bool initialized;
    Coroutine flashRoutine;
    float flashEndTime;
    Action pendingKillCallback;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (initialized) return;

        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer == null) return;

        normalMaterial = targetRenderer.material;

        if (hurtMaterialTemplate != null)
        {
            hurtMaterial = new Material(hurtMaterialTemplate);
        }
        else
        {
            hurtMaterial = new Material(normalMaterial);
            hurtMaterial.SetColor(BaseColorId, new Color(1f, 0.88f, 0.88f, 1f));
            if (hurtMaterial.HasProperty(EmissionColorId))
            {
                hurtMaterial.EnableKeyword("_EMISSION");
                hurtMaterial.SetColor(EmissionColorId, new Color(1.5f, 0.35f, 0.35f));
            }
        }

        initialized = true;
    }

    public void PlayHitFlash(bool isKillingBlow, Action onKillFlashComplete = null)
    {
        Initialize();
        if (!initialized) return;

        targetRenderer.material = hurtMaterial;

        float duration = isKillingBlow ? deathFlashDuration : hurtFlashDuration;
        flashEndTime = Time.time + duration;

        if (isKillingBlow)
        {
            pendingKillCallback = onKillFlashComplete;
        }

        if (flashRoutine == null)
        {
            flashRoutine = StartCoroutine(FlashTimerRoutine());
        }
    }

    IEnumerator FlashTimerRoutine()
    {
        while (Time.time < flashEndTime)
        {
            targetRenderer.material = hurtMaterial;
            yield return null;
        }

        if (pendingKillCallback != null)
        {
            Action callback = pendingKillCallback;
            pendingKillCallback = null;
            callback.Invoke();
        }
        else
        {
            RestoreNormalMaterial();
        }

        flashRoutine = null;
    }

    void RestoreNormalMaterial()
    {
        if (!initialized || targetRenderer == null || normalMaterial == null) return;
        targetRenderer.material = normalMaterial;
    }
}
