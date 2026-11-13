using UnityEngine;

public class EnergiaKapcsolo : MonoBehaviour
{
    [SerializeField] private GameObject bazisFenyek;

    private bool bekapcsolva = false;

    private void Start()
    {
        bazisFenyek.SetActive(false);
    }

    private void OnMouseDown()
    {
        bekapcsolva = !bekapcsolva;
        bazisFenyek.SetActive(bekapcsolva);
    }
}
