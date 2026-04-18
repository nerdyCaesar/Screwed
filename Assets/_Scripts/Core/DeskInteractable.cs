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
        Debug.Log($"[Desk] Worker {worker.OwnerClientId} started coding");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var worker = other.GetComponent<Worker>();
        if (worker == null) return;
        if (!worker.IsOwner) return;
        worker.SetAtDesk(false);
        Debug.Log($"[Desk] Worker {worker.OwnerClientId} left desk");
    }
}