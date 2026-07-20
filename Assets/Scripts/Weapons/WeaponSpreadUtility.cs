using UnityEngine;

public static class WeaponSpreadUtility
{
    /// <summary>
    /// Legacy spread values were small offsets added to the camera forward vector.
    /// For small values this is approximately spread in radians; convert to degrees.
    /// </summary>
    public static float OffsetToDegrees(float spreadOffset)
    {
        return Mathf.Atan(spreadOffset) * Mathf.Rad2Deg;
    }

    public static Vector3 GetSpreadDirection(Transform cameraTransform, float spreadDegrees)
    {
        if (spreadDegrees <= 0f)
        {
            return cameraTransform.forward;
        }

        Vector2 randomDisk = Random.insideUnitCircle;
        float yaw = randomDisk.x * spreadDegrees;
        float pitch = randomDisk.y * spreadDegrees;
        Quaternion spreadRotation = Quaternion.Euler(-pitch, yaw, 0f);
        return spreadRotation * cameraTransform.forward;
    }

    public static float SpreadDegreesToScreenRadius(Camera camera, float spreadDegrees)
    {
        if (camera == null || spreadDegrees <= 0f)
        {
            return 0f;
        }

        float spreadRadians = spreadDegrees * Mathf.Deg2Rad;
        float verticalFovRadians = camera.fieldOfView * Mathf.Deg2Rad;
        float screenHalfHeight = Screen.height * 0.5f;
        return Mathf.Tan(spreadRadians) / Mathf.Tan(verticalFovRadians * 0.5f) * screenHalfHeight;
    }
}
