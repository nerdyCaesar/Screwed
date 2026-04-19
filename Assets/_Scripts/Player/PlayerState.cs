using Unity.Netcode;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    public void RecordAction(string action)
    {
        // Placeholder for future analytics or telemetry hooks.
        _ = action;
    }
}