using UnityEngine;

public class PlayerCasting : MonoBehaviour
{

    public static float distanceFromTarget;
    [SerializeField] float toTarget;

   
    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 10f))
        {
            distanceFromTarget = hit.distance;
            toTarget = hit.distance;
        }
        else
        {
            distanceFromTarget = 999f;
            toTarget = 999f;
        }
    }
}
