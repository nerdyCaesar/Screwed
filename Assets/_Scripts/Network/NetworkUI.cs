using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        // ADD THIS LINE: If the Manager is gone (because we hit stop), abort drawing the UI!
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host (Me)", GUILayout.Width(200), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartHost();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Start Client (Friend)", GUILayout.Width(200), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartClient();
            }
        }

        GUILayout.EndArea();
    }
}