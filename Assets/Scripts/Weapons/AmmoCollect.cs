using UnityEngine;

public class AmmoCollect : MonoBehaviour
{
    [SerializeField] AmmoType ammoType = AmmoType.Pistol;
    [SerializeField] AudioSource ammoCollectSound;
    [SerializeField] int ammoVolumeReward = 15;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (ammoCollectSound != null) ammoCollectSound.Play();

        GlobalAmmo.AddReserve(ammoType, ammoVolumeReward);
        Destroy(gameObject, 0.4f);
    }
}
