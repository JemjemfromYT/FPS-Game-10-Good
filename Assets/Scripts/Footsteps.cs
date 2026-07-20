using System.Collections;
using UnityEngine;

public class Footsteps : MonoBehaviour
{
    [Header("Audio Configurations")]
    [SerializeField] AudioSource f1;
    [SerializeField] AudioSource f2;
    [SerializeField] AudioSource f3;
    [SerializeField] AudioSource f4;

    [Header("Tracking States")]
    [SerializeField] bool isStepping;
    [SerializeField] int soundNumber;

    private PlayerStamina playerStamina;

    void Start()
    {
        // Link up directly with your stamina script attached to the player
        playerStamina = GetComponent<PlayerStamina>();
    }

    void Update()
    {
        // Check if player is pressing any movement keys
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            if (isStepping == false)
            {
                isStepping = true;
                soundNumber = Random.Range(1, 5);
                StartCoroutine(Footstep());
            }
        }
    }

    IEnumerator Footstep()
    {
        // Play the random audio source step selection cleanly
        if (soundNumber == 1) f1.Play();
        if (soundNumber == 2) f2.Play();
        if (soundNumber == 3) f3.Play();
        if (soundNumber == 4) f4.Play();

        // FIX: Check the actual functional stamina state instead of the raw keyboard key
        bool isActuallySprinting = (playerStamina != null && playerStamina.IsSprinting);

        if (isActuallySprinting)
        {
            yield return new WaitForSeconds(0.3f); // Fast pacing for active running
        }
        else
        {
            yield return new WaitForSeconds(0.6f); // Slow pacing for normal walking
        }

        isStepping = false;
    }
}