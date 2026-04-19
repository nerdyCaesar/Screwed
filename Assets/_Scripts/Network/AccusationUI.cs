using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AccusationUI : MonoBehaviour
{
    public static AccusationUI Instance;

    [Header("UI References")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button submitButton;
    [SerializeField] TMP_Text timerLabel;
    [SerializeField] TMP_Text targetLabel;
    [SerializeField] TMP_Text deliberatingLabel;

    private TattletaleTerminal _terminal;
    private float _timeLeft;
    private bool _open;
    private ulong _accusedId;

    void Awake()
    {
        if (Instance == null) Instance = this;
        submitButton.onClick.AddListener(OnSubmit);
        panel.SetActive(false);
        if (deliberatingLabel) deliberatingLabel.gameObject.SetActive(false);
    }

    public void Open(TattletaleTerminal terminal)
    {
        _terminal = terminal;
        _timeLeft = 10f;
        _open = true;

        // find nearest other player to accuse
        var local = NetworkManager.Singleton.LocalClientId;
        var target = PlayerRegistry.Instance.GetNearestOtherPlayer(
            NetworkManager.Singleton.SpawnManager
                .GetLocalPlayerObject().transform.position, local);

        _accusedId = target != null ? target.OwnerClientId : 0;
        if (targetLabel) targetLabel.text = $"Accuse: Player {_accusedId}";

        inputField.text = "";
        panel.SetActive(true);
        inputField.ActivateInputField();
    }

    void Update()
    {
        if (!_open) return;
        _timeLeft -= Time.deltaTime;
        if (timerLabel) timerLabel.text = $"{Mathf.CeilToInt(_timeLeft)}s";
        if (_timeLeft <= 0) OnSubmit();
    }

    void OnSubmit()
    {
        if (!_open) return;
        _open = false;

        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) text = "They looked suspicious.";

        panel.SetActive(false);

        // show deliberating message
        if (deliberatingLabel)
        {
            deliberatingLabel.gameObject.SetActive(true);
            deliberatingLabel.text = "Judge is deliberating...";
        }

        _terminal.SubmitAccusationServerRpc(
            _accusedId,
            new FixedString512Bytes(text)
        );

        // hide deliberating after 3s
        Invoke(nameof(HideDeliberating), 3f);
    }

    void HideDeliberating()
    {
        if (deliberatingLabel)
            deliberatingLabel.gameObject.SetActive(false);
    }

    public void Close()
    {
        _open = false;
        panel.SetActive(false);
    }
}