using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using TMPro;

public enum MatchState { Waiting, Playing, Ended }

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [SerializeField] TattletaleTerminal terminal;

    public NetworkVariable<float> TimeRemaining = new(
        180f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<float> WorkerProgress = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<MatchState> State = new(
        MatchState.Waiting,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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

        UpdateTimerUI(TimeRemaining.Value);
        progressBarFill.fillAmount = WorkerProgress.Value / 100f;

        if (IsServer)
            State.Value = MatchState.Playing;
    }

    void Update()
    {
        if (!IsServer || State.Value != MatchState.Playing) return;

        TimeRemaining.Value -= Time.deltaTime;

        // terminal active between 2:00 and 1:00
        if (terminal != null)
        {
            bool shouldBeActive = TimeRemaining.Value < 120f && TimeRemaining.Value > 60f;
            if (terminal.gameObject.activeSelf != shouldBeActive)
                SetTerminalRpc(shouldBeActive);
        }

        if (TimeRemaining.Value <= 0)
        {
            TimeRemaining.Value = 0;
            EndMatch("Saboteurs");
        }
    }

    [Rpc(SendTo.Everyone)]
    void SetTerminalRpc(bool active)
    {
        if (terminal != null)
            terminal.gameObject.SetActive(active);
    }

    public void AddProgress(float amount)
    {
        if (!IsServer) return;
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
        Debug.Log($"{winner} WIN!");
    }

    void OnTimerChanged(float prev, float curr) => UpdateTimerUI(curr);
    void OnProgressChanged(float prev, float curr) => progressBarFill.fillAmount = curr / 100f;
    void OnStateChanged(MatchState prev, MatchState curr)
    {
        if (curr == MatchState.Playing) Debug.Log("Match started!");
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
    }
}