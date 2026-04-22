using System.Collections.Generic;
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
    [SerializeField] Button nextTargetButton;
    [SerializeField] TMP_Text timerLabel;
    [SerializeField] TMP_Text targetLabel;
    [SerializeField] TMP_Text deliberatingLabel;

    private TattletaleTerminal _terminal;
    private float _timeLeft;
    private bool _open;
    private ulong _accusedId;

    private List<BasePlayer> _otherPlayers = new();
    private int _targetIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Setup button listeners with null safety.
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmit);
        if (nextTargetButton != null)
            nextTargetButton.onClick.AddListener(CycleTarget);

        if (panel != null) panel.SetActive(false);
        if (deliberatingLabel != null) deliberatingLabel.gameObject.SetActive(false);
    }

    public void Open(TattletaleTerminal terminal)
    {
        if (panel == null) return;
        if (inputField == null) return;

        _terminal = terminal;
        _timeLeft = 10f;
        _open = true;

        // Build a target list that excludes the local player.
        _otherPlayers.Clear();
        ulong localId = NetworkManager.Singleton.LocalClientId;
        foreach (var p in PlayerRegistry.Instance.GetAllPlayers())
            if (p.OwnerClientId != localId)
                _otherPlayers.Add(p);

        _targetIndex = 0;
        UpdateTargetLabel();

        inputField.text = "";
        panel.SetActive(true);
        inputField.ActivateInputField();
    }

    public void CycleTarget()
    {
        if (_otherPlayers.Count == 0) return;
        _targetIndex = (_targetIndex + 1) % _otherPlayers.Count;
        UpdateTargetLabel();
    }

    void UpdateTargetLabel()
    {
        if (_otherPlayers.Count == 0)
        {
            if (targetLabel) targetLabel.text = "No other players found";
            _accusedId = 0;
            return;
        }
        var target = _otherPlayers[_targetIndex];
        _accusedId = target.OwnerClientId;
        if (targetLabel)
            targetLabel.text = $"Accuse: Player {_accusedId} ({target.Role.Value})";
    }

    void Update()
    {
        if (!_open) return;
        _timeLeft -= Time.deltaTime;
        if (timerLabel != null) timerLabel.text = $"{Mathf.CeilToInt(_timeLeft)}s";
        if (_timeLeft <= 0) OnSubmit();
    }

    void OnSubmit()
    {
        if (!_open) return;
        _open = false;

        string text = "";
        if (inputField != null)
            text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) text = "They looked suspicious.";

        Debug.Log($"[AccusationUI] Submit pressed. Accusation: '{text}' against Player {_accusedId}");

        if (panel != null)
            panel.SetActive(false);

        if (deliberatingLabel != null)
        {
            deliberatingLabel.gameObject.SetActive(true);
            deliberatingLabel.text = "Judge is deliberating...";
            Debug.Log($"[Accusation] Accuser: {NetworkManager.Singleton.LocalClientId}, Accused: {_accusedId}, Text: {text}");
        }

        if (_accusedId != 0 && _terminal != null)
            _terminal.SubmitAccusationServerRpc(
                _accusedId,
                new FixedString512Bytes(text)
            );

        Invoke(nameof(HideDeliberating), 3f);
    }

    void HideDeliberating()
    {
        if (deliberatingLabel != null)
            deliberatingLabel.gameObject.SetActive(false);
    }

    public void Close()
    {
        _open = false;
        panel.SetActive(false);
    }
}