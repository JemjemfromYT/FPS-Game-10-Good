using UnityEngine;

[DisallowMultipleComponent]
public class CrosshairSpreadDisplay : MonoBehaviour
{
    enum CrosshairLayout
    {
        FourLines,
        Circle
    }

    [SerializeField] float minGapPixels = 4f;
    [SerializeField] float circleDiameterMultiplier = 2f;

    CrosshairLayout layout;
    RectTransform leftLine;
    RectTransform rightLine;
    RectTransform topLine;
    RectTransform bottomLine;
    RectTransform circleRect;

    Vector2 leftBasePosition;
    Vector2 rightBasePosition;
    Vector2 topBasePosition;
    Vector2 bottomBasePosition;
    Vector2 circleBaseSize;
    bool cached;

    void Awake()
    {
        CacheLayout();
    }

    void CacheLayout()
    {
        if (cached) return;

        foreach (Transform child in transform)
        {
            string childName = child.name.ToLowerInvariant();
            RectTransform rect = child as RectTransform;
            if (rect == null) continue;

            if (childName.Contains("left"))
            {
                leftLine = rect;
                leftBasePosition = rect.anchoredPosition;
            }
            else if (childName.Contains("right"))
            {
                rightLine = rect;
                rightBasePosition = rect.anchoredPosition;
            }
            else if (childName.Contains("top"))
            {
                topLine = rect;
                topBasePosition = rect.anchoredPosition;
            }
            else if (childName.Contains("bottom"))
            {
                bottomLine = rect;
                bottomBasePosition = rect.anchoredPosition;
            }
        }

        if (leftLine != null || rightLine != null || topLine != null || bottomLine != null)
        {
            layout = CrosshairLayout.FourLines;
        }
        else
        {
            layout = CrosshairLayout.Circle;
            circleRect = transform as RectTransform;
            if (circleRect != null)
            {
                circleBaseSize = circleRect.sizeDelta;
            }
        }

        cached = true;
    }

    public void ApplySpread(float spreadDegrees, Camera camera)
    {
        CacheLayout();
        float gapPixels = WeaponSpreadUtility.SpreadDegreesToScreenRadius(camera, spreadDegrees);
        gapPixels = Mathf.Max(gapPixels, minGapPixels);

        if (layout == CrosshairLayout.FourLines)
        {
            ApplyLineGap(leftLine, leftBasePosition, new Vector2(-gapPixels, 0f));
            ApplyLineGap(rightLine, rightBasePosition, new Vector2(gapPixels, 0f));
            ApplyLineGap(topLine, topBasePosition, new Vector2(0f, gapPixels));
            ApplyLineGap(bottomLine, bottomBasePosition, new Vector2(0f, -gapPixels));
            return;
        }

        if (circleRect == null) return;

        float baseRadius = Mathf.Max(circleBaseSize.x, circleBaseSize.y) * 0.5f;
        if (baseRadius <= 0f) baseRadius = 1f;

        float targetDiameter = Mathf.Max(gapPixels * circleDiameterMultiplier, minGapPixels * 2f);
        float scale = targetDiameter / (baseRadius * 2f);

        // FIXED: Scale the physical transform instead of the invisible bounding box!
        // This forces any child pictures (like your RawImage) to visually zoom in and out.
        circleRect.localScale = new Vector3(scale, scale, 1f);
    }

    static void ApplyLineGap(RectTransform line, Vector2 basePosition, Vector2 axisGap)
    {
        if (line == null) return;

        float baseGap = Mathf.Abs(basePosition.x) > Mathf.Abs(basePosition.y)
            ? Mathf.Abs(basePosition.x)
            : Mathf.Abs(basePosition.y);

        if (baseGap <= 0f) baseGap = 1f;
        float scale = Mathf.Abs(axisGap.x) > 0f
            ? Mathf.Abs(axisGap.x) / baseGap
            : Mathf.Abs(axisGap.y) / baseGap;

        line.anchoredPosition = basePosition * scale;
    }
}