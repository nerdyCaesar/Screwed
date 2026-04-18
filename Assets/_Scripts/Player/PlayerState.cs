using Unity.Netcode;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    public void RecordAction(string action)
    {
        Debug.Log($"[PlayerState] {action}");
    }
}