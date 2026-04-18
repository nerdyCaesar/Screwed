using Unity.Netcode;
using UnityEngine;

public class JudgeNPC : NetworkBehaviour
{
    public void SetDistracted(float duration)
    {
        Debug.Log($"[JudgeNPC] Distracted for {duration}s");
    }
}