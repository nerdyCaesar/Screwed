using Unity.Netcode;
using UnityEngine;

public class DeskInteractable : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var worker = other.GetComponent<Worker>();
        if (worker == null) return;
        if (!worker.IsOwner) return;
        worker.SetAtDesk(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var worker = other.GetComponent<Worker>();
        if (worker == null) return;
        if (!worker.IsOwner) return;
        worker.SetAtDesk(false);
    }
}