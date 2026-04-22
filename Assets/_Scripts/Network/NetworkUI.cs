using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] GameObject networkPanel;
    [SerializeField] TMP_InputField ipInputField;
    [SerializeField] TMP_Text statusText;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;

    void Start()
    {
        if (ipInputField == null)
        {
            Debug.LogError("[NetworkUI] ipInputField is not assigned in the inspector!");
            return;
        }

        if (hostButton != null)
        {
            hostButton.onClick.RemoveListener(StartHost);
            hostButton.onClick.AddListener(StartHost);
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(StartClient);
            joinButton.onClick.AddListener(StartClient);
        }

        ipInputField.text = "";
        ipInputField.placeholder.GetComponent<TMP_Text>().text
            = "Enter ngrok address (e.g. 0.tcp.ngrok.io:12345)";

        // Ensure input field is ready to receive input
        ipInputField.ActivateInputField();
    }

    public void StartHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777);
        NetworkManager.Singleton.StartHost();

        if (statusText) statusText.text = "Hosting! Run: ngrok tcp 7777\nShare the address with friends.";
        if (hostButton) hostButton.interactable = false;
        if (joinButton) joinButton.interactable = false;
        networkPanel.SetActive(false);
    }

    public async void StartClient()
    {
        string input = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            if (statusText) statusText.text = "Please enter an address!";
            return;
        }

        string ip = input;
        ushort port = 7777;

        // handle host:port format from ngrok
        if (input.Contains(":"))
        {
            var parts = input.Split(':');
            ip = parts[0];
            ushort.TryParse(parts[1], out port);
        }

        if (statusText) statusText.text = $"Resolving {ip}...";

        // resolve hostname to IP
        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(ip);
            if (addresses.Length > 0)
            {
                ip = addresses[0].ToString();
                Debug.Log($"[Network] Resolved to: {ip}:{port}");
            }
        }
        catch
        {
            Debug.Log("[Network] Using address directly");
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);
        NetworkManager.Singleton.StartClient();

        if (statusText) statusText.text = $"Connecting to {ip}:{port}...";
        if (hostButton) hostButton.interactable = false;
        if (joinButton) joinButton.interactable = false;
        networkPanel.SetActive(false);
    }
}