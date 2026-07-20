using UnityEngine;

public class WeaponDrop : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public GameObject floorPrefab;
    public GameObject actionText;
    public float throwForce = 8f;

    private WeaponFire fireScript;

    void Start()
    {
        fireScript = GetComponent<WeaponFire>();
        EnsureDropReferences();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Drop();
        }
    }

    public void Drop()
    {
        EnsureDropReferences();

        if (floorPrefab == null)
        {
            Debug.LogError("WeaponDrop: No floor prefab assigned on " + gameObject.name + ". Check Weapon_Container setup.");
            return;
        }

        Transform cameraTransform = ResolveCameraTransform();
        if (cameraTransform == null)
        {
            Debug.LogError("WeaponDrop: Could not find the player camera for " + gameObject.name);
            return;
        }

        if (fireScript == null)
        {
            fireScript = GetComponent<WeaponFire>();
        }

        if (fireScript != null)
        {
            fireScript.CancelWeaponActions();
        }

        Vector3 spawnPos = cameraTransform.position + (cameraTransform.forward * 1.2f);
        GameObject droppedItem = Instantiate(floorPrefab, spawnPos, cameraTransform.rotation);
        if (droppedItem == null)
        {
            Debug.LogError("WeaponDrop: Failed to spawn floor weapon for " + gameObject.name);
            return;
        }

        Animator anim = droppedItem.GetComponent<Animator>();
        if (anim == null) anim = droppedItem.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
        }

        MeshCollider meshCol = droppedItem.GetComponent<MeshCollider>();
        if (meshCol == null) meshCol = droppedItem.GetComponentInChildren<MeshCollider>();
        if (meshCol != null)
        {
            meshCol.convex = true;
        }

        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null) rb = droppedItem.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;

        SetRigidbodyVelocityZero(rb);
        rb.AddForce(cameraTransform.forward * throwForce, ForceMode.Impulse);

        WeaponPickup pickupScript = droppedItem.GetComponent<WeaponPickup>();
        if (pickupScript != null)
        {
            pickupScript.actionText = actionText;
        }

        gameObject.SetActive(false);
    }

    public void EnsureDropReferences()
    {
        if (fireScript == null)
        {
            fireScript = GetComponent<WeaponFire>();
        }

        if (actionText == null)
        {
            actionText = GameObject.Find("ActionText");
        }

        if (floorPrefab == null)
        {
            WeaponContainerSetup setup = GetComponentInParent<WeaponContainerSetup>();
            if (setup != null)
            {
                floorPrefab = setup.GetFloorPrefab(gameObject.name);
            }
        }

        if (playerCamera == null)
        {
            Transform cameraTransform = ResolveCameraTransform();
            if (cameraTransform != null)
            {
                playerCamera = cameraTransform;
            }
        }
    }

    Transform ResolveCameraTransform()
    {
        if (playerCamera != null)
        {
            return playerCamera;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Camera playerCam = player.GetComponentInChildren<Camera>(true);
            if (playerCam != null)
            {
                return playerCam.transform;
            }
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera cam in cameras)
        {
            if (cam != null && cam.enabled)
            {
                return cam.transform;
            }
        }

        return null;
    }

    private void SetRigidbodyVelocityZero(Rigidbody rb)
    {
#if UNITY_2023_1_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
    }
}
