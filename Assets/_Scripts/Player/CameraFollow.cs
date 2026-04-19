using UnityEngine;
using Unity.Cinemachine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] CinemachineCamera cam;

    void Update()
    {
        var players = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsOwner)
            {
                cam.Follow = p.transform;
                return;
            }
        }
    }
}