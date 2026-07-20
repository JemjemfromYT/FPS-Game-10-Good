using System.Collections;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject zombiePrefab;
    public GameObject[] extraEnemyPrefabs;
    public int extraEnemiesStartWave = 3;
    [Range(0f, 1f)]
    public float extraEnemyChance = 0.35f;

    public Transform[] spawnPoints;
    public float timeBetweenSpawns = 1.5f;

    [Header("UI References")]
    public TMP_Text waveText;
    public TMP_Text promptText;

    private int currentWave = 0;
    private int zombiesToSpawn;
    private bool isSpawning = false;
    private bool waitingForPlayer = false;

    public static bool isWaveActive = false;

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

    // Public so MobileControls.OnStartWaveButton() can call it directly via SendMessage,
    // and so StoreManager can also call it if needed.
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

        bool canSpawnExtra = currentWave >= extraEnemiesStartWave
                          && extraEnemyPrefabs != null
                          && extraEnemyPrefabs.Length > 0;

        for (int i = 0; i < zombiesToSpawn; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject prefabToSpawn = zombiePrefab;
            if (canSpawnExtra && Random.value < extraEnemyChance)
                prefabToSpawn = extraEnemyPrefabs[Random.Range(0, extraEnemyPrefabs.Length)];

            if (prefabToSpawn != null)
                Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }
}
