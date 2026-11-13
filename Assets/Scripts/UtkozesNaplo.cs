using UnityEngine;

public class UtkozesNaplo : MonoBehaviour
{
    private void OnCollisionEnter(Collision utkozes)
    {
        Debug.Log("Ütközés: " + utkozes.gameObject.name + " (Collider: " + utkozes.collider.name + ")");
    }
}