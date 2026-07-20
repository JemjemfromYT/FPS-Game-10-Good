using UnityEngine;

public static class DamageIndicatorSpawner
{
    public static void Spawn(Vector3 worldPosition, float damage, GameObject prefab)
    {
        FloatingDamageIndicator.Create(worldPosition, damage, prefab);
    }
}
