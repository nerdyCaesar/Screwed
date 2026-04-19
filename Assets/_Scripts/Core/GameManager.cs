using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using TMPro;

public enum MatchState { Waiting, Playing, Ended }

public class GameManager : NetworkBehaviour
{

    [SerializeField] GameObject endPanel;
    [SerializeField] TMP_Text winnerText;
    public static GameManager Instance;
    [SerializeField] TattletaleTerminal terminal;

    public NetworkVariable<float> TimeRemaining = new(180f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> WorkerProgress = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<MatchState> State = new(MatchState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> TerminalVisible = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float _progressPausedTimer = 0f;
    public bool ProgressPaused => _progressPausedTimer > 0f;

    [Header("UI")]
    [SerializeField] TMP_Text timerLabel;
    [SerializeField] UnityEngine.UI.Image progressBarFill;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        TimeRemaining.OnValueChanged += OnTimerChanged;
        WorkerProgress.OnValueChanged += OnProgressChanged;
        State.OnValueChanged += OnStateChanged;
        TerminalVisible.OnValueChanged += OnTerminalVisibilityChanged;

        UpdateTimerUI(TimeRemaining.Value);
        progressBarFill.fillAmount = WorkerProgress.Value / 100f;
        OnTerminalVisibilityChanged(false, TerminalVisible.Value);

        if (IsServer)
            State.Value = MatchState.Playing;
    }

    void OnTerminalVisibilityChanged(bool prev, bool curr)
    {
        if (terminal == null) return;
        var sr = terminal.GetComponent<SpriteRenderer>();
        var col = terminal.GetComponent<Collider2D>();
        if (sr) sr.enabled = curr;
        if (col) col.enabled = curr;
    }

    void Update()
    {
        if (!IsServer || State.Value != MatchState.Playing) return;

        TimeRemaining.Value -= Time.deltaTime;

        if (_progressPausedTimer > 0f)
            _progressPausedTimer -= Time.deltaTime;

        if (terminal != null)
        {
            bool shouldBeActive = TimeRemaining.Value < 120f && TimeRemaining.Value > 60f;
            if (TerminalVisible.Value != shouldBeActive)
                TerminalVisible.Value = shouldBeActive;
        }

        if (TimeRemaining.Value <= 0)
        {
            TimeRemaining.Value = 0;
            EndMatch("Saboteurs");
        }
    }

    public void PauseProgress(float duration)
    {
        if (!IsServer) return;
        _progressPausedTimer = duration;
    }

    public void AddProgress(float amount)
    {
        if (!IsServer) return;
        if (ProgressPaused) return;
        WorkerProgress.Value = Mathf.Clamp(WorkerProgress.Value + amount, 0f, 100f);
        if (WorkerProgress.Value >= 100f)
            EndMatch("Workers");
    }

    void EndMatch(string winningSide)
    {
        if (State.Value == MatchState.Ended) return;
        State.Value = MatchState.Ended;
        ShowEndScreenRpc(winningSide);
    }

    [Rpc(SendTo.Everyone)]
    void ShowEndScreenRpc(FixedString32Bytes winner)
    {
        endPanel.SetActive(true);
        winnerText.text = $"{winner} WIN!";
        Time.timeScale = 0f;
    }

    void OnTimerChanged(float prev, float curr) => UpdateTimerUI(curr);
    void OnProgressChanged(float prev, float curr) => progressBarFill.fillAmount = curr / 100f;
    void OnStateChanged(MatchState prev, MatchState curr)
    {
        // Hook for future state-change reactions on clients.
    }

    void UpdateTimerUI(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        timerLabel.text = $"{m}:{s:00}";
        if (seconds <= 30) timerLabel.color = Color.red;
    }

    public override void OnNetworkDespawn()
    {
        TimeRemaining.OnValueChanged -= OnTimerChanged;
        WorkerProgress.OnValueChanged -= OnProgressChanged;
        State.OnValueChanged -= OnStateChanged;
        TerminalVisible.OnValueChanged -= OnTerminalVisibilityChanged;
    }
}