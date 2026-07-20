using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] float pickupRange = 2.5f;
    [SerializeField] string weaponNameInHierarchy = "Weapon_M9";

    [Header("References (Auto-Assigned at Runtime)")]
    public GameObject actionText;
    public GameObject gunInHand;
    public GameObject gunMechanics;

    private GameObject playerObj;
    private static WeaponPickup currentPromptOwner;

    void Start()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");

        if (actionText == null)
        {
            actionText = GameObject.Find("ActionText");
        }

        DisableFloorWeaponScripts();
        AutoAssignConnections();
    }

    void Update()
    {
        if (playerObj == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerObj.transform.position);
        if (distanceToPlayer > pickupRange)
        {
            ClearPromptIfOwner();
            return;
        }

        if (!IsPlayerLookingAtThis())
        {
            ClearPromptIfOwner();
            return;
        }

        if (PlayerHasEquippedWeapon())
        {
            ClearPromptIfOwner();
            return;
        }

        currentPromptOwner = this;
        if (actionText != null) actionText.SetActive(true);

        if (Input.GetKeyDown(KeyCode.E))
        {
            ExecutePickup();
        }
    }

    void ExecutePickup()
    {
        if (PlayerHasEquippedWeapon())
        {
            return;
        }

        ClearPromptIfOwner();

        if (gunInHand != null)
        {
            gunInHand.SetActive(true);

            WeaponFire fireScript = gunInHand.GetComponent<WeaponFire>();
            if (fireScript != null)
            {
                fireScript.enabled = true;
                fireScript.CancelWeaponActions();
            }
        }
        else
        {
            Debug.LogError("Pickup System Error: Could not locate '" + weaponNameInHierarchy + "' on the player!");
            return;
        }

        Destroy(gameObject);
    }

    bool PlayerHasEquippedWeapon()
    {
        Transform weaponContainer = FindWeaponContainer();
        if (weaponContainer == null) return false;

        WeaponFire[] weapons = weaponContainer.GetComponentsInChildren<WeaponFire>(true);
        foreach (WeaponFire weapon in weapons)
        {
            if (weapon.gameObject.activeInHierarchy && weapon.enabled)
            {
                return true;
            }
        }

        return false;
    }

    void AutoAssignConnections()
    {
        if (playerObj == null || string.IsNullOrEmpty(weaponNameInHierarchy)) return;

        Transform[] children = playerObj.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name != weaponNameInHierarchy) continue;

            gunInHand = child.gameObject;
            gunMechanics = child.gameObject;
            return;
        }

        Debug.LogWarning("WeaponPickup could not find '" + weaponNameInHierarchy + "' under the player.");
    }

    bool IsPlayerLookingAtThis()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange + 1f)) return false;

        Transform hitTransform = hit.collider.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    Transform FindWeaponContainer()
    {
        if (playerObj == null) return null;

        Transform[] children = playerObj.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == "Weapon_Container") return child;
        }

        return null;
    }

    void DisableFloorWeaponScripts()
    {
        WeaponFire strayFire = GetComponent<WeaponFire>();
        if (strayFire != null) strayFire.enabled = false;

        WeaponDrop strayDrop = GetComponent<WeaponDrop>();
        if (strayDrop != null) strayDrop.enabled = false;

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        MeshCollider meshCol = GetComponent<MeshCollider>();
        if (meshCol != null) meshCol.convex = true;
    }

    void ClearPromptIfOwner()
    {
        if (currentPromptOwner == this)
        {
            if (actionText != null) actionText.SetActive(false);
            currentPromptOwner = null;
        }
    }

    void OnDestroy()
    {
        ClearPromptIfOwner();
    }

    void OnDisable()
    {
        ClearPromptIfOwner();
    }
}
