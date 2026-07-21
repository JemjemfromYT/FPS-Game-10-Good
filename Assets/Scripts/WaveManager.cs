// ============================================================
//  WaveManager.cs  —  UPDATED
//  Drop into: Assets/Scripts/WaveManager.cs
//
//  WHAT CHANGED:
//  The old "Extra Enemy Prefabs" array + single "Extra Enemies
//  Start Wave" number is replaced by a new list called
//  "Extra Enemies". Each entry in the list has:
//    • Prefab          → drag the enemy prefab here
//    • Start From Wave → the earliest wave this enemy can appear
//    • Spawn Chance    → 0–1 probability per spawn slot (default 0.35)
//
//  Example setup:
//    Element 0 → ZombieExploder, Start From Wave: 2, Chance: 0.3
//    Element 1 → ZombieThrower,  Start From Wave: 4, Chance: 0.2
//
//  HOW TO RE-WIRE IN THE INSPECTOR (takes ~1 minute):
//  ─────────────────────────────────────────────────────────────
//  1. Replace WaveManager.cs with this file.
//  2. Select WaveManager in the Hierarchy.
//  3. You will see "Extra Enemies" list in the Inspector.
//     The old "Extra Enemy Prefabs" and "Extra Enemies Start Wave"
//     fields are gone — re-add your prefabs there with their
//     individual wave thresholds.
//  4. Everything else (Zombie Prefab, Spawn Points, Wave Text,
//     Prompt Text, Time Between Spawns) is unchanged.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    // ── Serializable entry: one enemy type with its own wave gate ─────────────
    [System.Serializable]
    public class ExtraEnemyEntry
    {
        [Tooltip("The enemy prefab to spawn")]
        public GameObject prefab;

        [Tooltip("This enemy type only appears on this wave or later")]
        public int startFromWave = 3;

        [Range(0f, 1f)]
        [Tooltip("Chance (0–1) that any given spawn slot becomes this enemy type")]
        public float spawnChance = 0.35f;
    }

    [Header("Wave Settings")]
    public GameObject zombiePrefab;

    [Tooltip("Each entry is an enemy type with its own start wave and spawn chance")]
    public List<ExtraEnemyEntry> extraEnemies = new List<ExtraEnemyEntry>();

    public Transform[] spawnPoints;
    public float timeBetweenSpawns = 1.5f;

    [Header("UI References")]
    public TMP_Text waveText;
    public TMP_Text promptText;

    // ── private state ─────────────────────────────────────────────────────────
    private int currentWave = 0;
    private int zombiesToSpawn = 0;
    private bool isSpawning = false;
    private bool waitingForPlayer = false;

    public static bool isWaveActive = false;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (waveText != null) waveText.text = "WAVE 0";
        PrepareNextWave();
    }

    void Update()
    {
        if (waitingForPlayer)
        {
            if (Input.GetKeyDown(KeyCode.P))
                StartNextWave();
            return;
        }

        if (!isSpawning)
        {
            if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
                PrepareNextWave();
        }
    }

    void PrepareNextWave()
    {
        waitingForPlayer = true;
        isWaveActive = false;

        if (promptText != null)
        {
            promptText.text = "Press 'P' to Start Wave " + (currentWave + 1) + "\n[Press 'B' to Open Store]";
            promptText.gameObject.SetActive(true);
        }
    }

    // Public so MobileControls.OnStartWaveButton() and StoreManager can call it.
    public void StartNextWave()
    {
        waitingForPlayer = false;
        isWaveActive = true;

        if (promptText != null)
            promptText.gameObject.SetActive(false);

        currentWave++;
        if (waveText != null) waveText.text = "WAVE " + currentWave;

        zombiesToSpawn = currentWave * 2;
        StartCoroutine(SpawnZombies());
    }

    IEnumerator SpawnZombies()
    {
        isSpawning = true;
        yield return new WaitForSeconds(0.5f);

        // Build the list of enemy entries that are unlocked this wave.
        List<ExtraEnemyEntry> available = new List<ExtraEnemyEntry>();
        foreach (ExtraEnemyEntry entry in extraEnemies)
        {
            if (entry.prefab != null && currentWave >= entry.startFromWave)
                available.Add(entry);
        }

        for (int i = 0; i < zombiesToSpawn; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefabToSpawn = PickPrefab(available);

            if (prefabToSpawn != null)
                Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }

    // ── Pick which enemy to spawn for this slot ───────────────────────────────
    // Rolls each unlocked extra enemy's individual chance in order.
    // If none trigger, falls back to the regular zombie.
    GameObject PickPrefab(List<ExtraEnemyEntry> available)
    {
        // Shuffle so no single entry always gets first priority.
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            ExtraEnemyEntry tmp = available[i];
            available[i] = available[j];
            available[j] = tmp;
        }

        foreach (ExtraEnemyEntry entry in available)
        {
            if (Random.value < entry.spawnChance)
                return entry.prefab;
        }

        return zombiePrefab; // default: regular zombie
    }
}
