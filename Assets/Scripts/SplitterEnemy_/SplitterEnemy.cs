// ============================================================
//  SplitterEnemy.cs  —  NEW FILE
//  Place in: Assets/Scripts/SplitterEnemy.cs
//
//  Add this script to an enemy prefab that also has EnemyAi.
//  When it dies it spawns 3 smaller versions of itself.
//  The mini versions do NOT split again.
//
//  HOW TO SET UP (takes about 4 minutes):
//  ─────────────────────────────────────────────────────────────
//  STEP 1 — Create the MINI prefab:
//    a. Duplicate your Zombie prefab → rename it "ZombieSplitterMini"
//    b. Set its Transform Scale to (0.5, 0.5, 0.5)
//    c. Add SplitterEnemy script to it
//    d. Check the "Is Mini" checkbox (so it doesn't split again)
//    e. Reduce EnemyAi → Health to something small (e.g. 15)
//    f. Drag it from the Hierarchy into Assets/Prefab to save it
//       as a prefab, then delete the Hierarchy copy
//
//  STEP 2 — Create the BIG (splitter) prefab:
//    a. Duplicate your Zombie prefab → rename it "ZombieSplitter"
//    b. Add SplitterEnemy script to it
//    c. Leave "Is Mini" UNCHECKED
//    d. Drag "ZombieSplitterMini" prefab into the "Mini Prefab" slot
//    e. Set EnemyAi → Health to something higher (e.g. 80)
//    f. Optionally give it a different material color so it
//       looks distinct (e.g. purple)
//    g. Save as prefab
//
//  STEP 3 — Add to WaveManager:
//    a. Select WaveManager in the Hierarchy
//    b. In Extra Enemies, add a new entry
//    c. Drag ZombieSplitter → Prefab, set Start From Wave, Chance
//
//  STEP 4 — Add EnemyAi.cs update:
//    Replace EnemyAi.cs with EnemyAi_FIXED.txt (it adds the
//    SplitterEnemy death hook exactly like ExplodingEnemy).
// ============================================================

using UnityEngine;

public class SplitterEnemy : MonoBehaviour
{
    [Header("Split Settings")]
    [Tooltip("The smaller enemy prefab that spawns on death. Must be set — see setup notes above.")]
    public GameObject miniPrefab;

    [Tooltip("How many mini enemies to spawn")]
    public int splitCount = 3;

    [Tooltip("Check this on the MINI version so it does not split again")]
    public bool isMini = false;

    [Tooltip("How far from the death point to scatter the minis")]
    public float scatterRadius = 0.8f;

    // ── Called by EnemyAi.TakeDamage on death (same pattern as ExplodingEnemy) ──
    public void TriggerSplit()
    {
        if (isMini) return;           // minis never split
        if (miniPrefab == null) return; // nothing to spawn

        Vector3 deathPos = transform.position;

        for (int i = 0; i < splitCount; i++)
        {
            // Spread the minis evenly around the death position
            float angle = (360f / splitCount) * i;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * scatterRadius;
            Vector3 spawnPos = deathPos + offset + Vector3.up * 0.1f;

            Instantiate(miniPrefab, spawnPos, Quaternion.Euler(0f, angle, 0f));
        }
    }

    // Gizmo so you can see the scatter radius in Scene view
    void OnDrawGizmosSelected()
    {
        if (isMini) return;
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, scatterRadius);
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, scatterRadius);
    }
}
