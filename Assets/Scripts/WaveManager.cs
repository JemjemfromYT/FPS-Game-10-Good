using System.Collections;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;
    public float timeBetweenSpawns = 1.5f;

    [Header("UI References")]
    public TMP_Text waveText;       // Permanent display (e.g., "WAVE 1")
    public TMP_Text promptText;     // Temporary display (e.g., "Press 'P' to Start...")

    private int currentWave = 0;
    private int zombiesToSpawn;
    private bool isSpawning = false;
    private bool waitingForPlayer = false;

    // Add this new static variable near the top of the variables list:
    public static bool isWaveActive = false;

    void Start()
    {
        // Setup the initial text layout
        if (waveText != null) waveText.text = "WAVE 0";
        PrepareNextWave();
    }

    void Update()
    {
        // 1. If we are waiting for the player to press P
        if (waitingForPlayer)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                StartNextWave();
            }
            return;
        }

        // 2. If a wave is running, check if all zombies are dead
        if (!isSpawning)
        {
            if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
            {
                PrepareNextWave();
            }
        }
    }

    void PrepareNextWave()
    {
        waitingForPlayer = true;
        isWaveActive = false; // NEW: Wave is over, shop can open!

        if (promptText != null)
        {
            promptText.text = "Press 'P' to Start Wave " + (currentWave + 1) + "\n[Press 'B' to Open Store]";
            promptText.gameObject.SetActive(true);
        }
    }

    void StartNextWave()
    {
        waitingForPlayer = false;
        isWaveActive = true; // NEW: Wave started, close/disable shopping inputs

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }

        currentWave++;
        if (waveText != null) waveText.text = "WAVE " + currentWave;

        zombiesToSpawn = currentWave * 2;
        StartCoroutine(SpawnZombies());
    }

    IEnumerator SpawnZombies()
    {
        isSpawning = true;
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < zombiesToSpawn; i++)
        {
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(zombiePrefab, randomSpawnPoint.position, randomSpawnPoint.rotation);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }
}