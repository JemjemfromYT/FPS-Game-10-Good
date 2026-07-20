using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to your Main Camera.
/// ExplodingEnemy calls TriggerShake() automatically on explosion.
/// You can also call it from any other script for other effects.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private Vector3 _originalLocalPos;
    private Coroutine _shakeRoutine;

    void Start()
    {
        _originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Call this to shake the camera.
    /// magnitude = how far it moves (0.3 is noticeable but not nauseating)
    /// duration  = seconds the shake lasts
    /// </summary>
    public void TriggerShake(float magnitude, float duration)
    {
        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(Shake(magnitude, duration));
    }

    private IEnumerator Shake(float magnitude, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float dampened = magnitude * (1f - progress);   // fades out over time

            float x = Random.Range(-1f, 1f) * dampened;
            float y = Random.Range(-1f, 1f) * dampened;

            transform.localPosition = _originalLocalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _originalLocalPos;
        _shakeRoutine = null;
    }
}
