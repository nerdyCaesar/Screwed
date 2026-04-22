using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] TMP_InputField ipInputField;
    [SerializeField] TMP_Text statusText;

    void Start()
    {
        // default IP for local testing
        ipInputField.text = "127.0.0.1";
    }

    public void StartHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777);
        NetworkManager.Singleton.StartHost();
        if (statusText) statusText.text = "Hosting on port 7777...";
    }

    public void StartClient()
    {
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, 7777);
        NetworkManager.Singleton.StartClient();
        if (statusText) statusText.text = $"Connecting to {ip}...";
    }
}