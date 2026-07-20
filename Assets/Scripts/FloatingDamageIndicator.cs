using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingDamageIndicator : MonoBehaviour
{
    const float DisplayDuration = 1.2f;
    const float RiseSpeed = 1.5f;

    TextMeshPro worldText;
    TextMeshProUGUI uiText;
    Color startColor;

    public static FloatingDamageIndicator Create(Vector3 worldPosition, float damage, GameObject prefab)
    {
        GameObject indicatorObject;

        if (prefab != null)
        {
            indicatorObject = Instantiate(prefab, worldPosition, Quaternion.identity);
            DamageIndicatorText indicatorText = indicatorObject.GetComponent<DamageIndicatorText>();
            if (indicatorText != null)
            {
                indicatorText.SetDamageText(damage);
            }
        }
        else
        {
            indicatorObject = new GameObject("DamageIndicator");
            indicatorObject.transform.position = worldPosition + Vector3.up * 1.5f;

            TextMeshPro text = indicatorObject.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.text = Mathf.RoundToInt(damage).ToString();
            text.fontSize = 6f;
            text.color = new Color(1f, 0.85f, 0.2f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(2f, 1f);
        }

        FloatingDamageIndicator indicator = indicatorObject.GetComponent<FloatingDamageIndicator>();
        if (indicator == null)
        {
            indicator = indicatorObject.AddComponent<FloatingDamageIndicator>();
        }

        indicator.Begin();
        indicator.FaceMainCamera();
        return indicator;
    }

    void Begin()
    {
        worldText = GetComponent<TextMeshPro>();
        uiText = GetComponent<TextMeshProUGUI>();
        if (uiText == null) uiText = GetComponentInChildren<TextMeshProUGUI>();

        if (worldText != null)
        {
            startColor = worldText.color;
        }
        else if (uiText != null)
        {
            startColor = uiText.color;
        }
        else
        {
            startColor = Color.white;
        }

        FaceMainCamera();
        StartCoroutine(AnimateAndDestroy());
    }

    void FaceMainCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        transform.rotation = Quaternion.LookRotation(camera.transform.forward, Vector3.up);
    }

    IEnumerator AnimateAndDestroy()
    {
        float timer = 0f;

        while (timer < DisplayDuration)
        {
            transform.position += Vector3.up * RiseSpeed * Time.deltaTime;
            FaceMainCamera();

            float alpha = 1f - (timer / DisplayDuration);
            ApplyAlpha(alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    void ApplyAlpha(float alpha)
    {
        if (worldText != null)
        {
            worldText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }

        if (uiText != null)
        {
            uiText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }
    }
}
