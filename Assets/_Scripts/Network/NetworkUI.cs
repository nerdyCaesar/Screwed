using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        // Only show buttons if we aren't connected yet
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host (Me)", GUILayout.Width(200), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartHost();
            }

            GUILayout.Space(10); // Adds a little gap between buttons

            if (GUILayout.Button("Start Client (Friend)", GUILayout.Width(200), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartClient();
            }
        }

        GUILayout.EndArea();
    }
}