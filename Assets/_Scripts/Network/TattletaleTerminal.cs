using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TattletaleTerminal : NetworkBehaviour
{
    [Header("Timing")]
    [SerializeField] float minSpawnInterval = 20f;
    [SerializeField] float maxSpawnInterval = 45f;
    [SerializeField] float activeWindow = 15f;

    [Header("References")]
    [SerializeField] GameObject visualRoot;
    [SerializeField] GameObject interactPrompt;

    // NetworkVariables
    public NetworkVariable<bool> IsVisible = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<bool> IsOccupied = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<ulong> OccupantClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine _cycleRoutine;

    public override void OnNetworkSpawn()
    {
        IsVisible.OnValueChanged += OnVisibilityChanged;
        IsOccupied.OnValueChanged += OnOccupiedChanged;

        if (visualRoot) visualRoot.SetActive(IsVisible.Value);
        if (interactPrompt) interactPrompt.SetActive(false);

        if (IsServer)
            _cycleRoutine = StartCoroutine(SpawnCycle());

        Debug.Log($"[Terminal] OnNetworkSpawn fired — IsServer: {IsServer}");
    }
    // ── Server: random spawn cycle ────────────────────────────
    IEnumerator SpawnCycle()
    {
        while (true)
        {
            // wait random interval
            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            Debug.Log($"[Terminal] Next appearance in {wait:F1}s");
            yield return new WaitForSeconds(wait);

            // make visible
            IsVisible.Value = true;
            Debug.Log("[Terminal] Appeared!");

            yield return new WaitForSeconds(activeWindow);

            if (!IsOccupied.Value)
            {
                ResetTerminal();
                Debug.Log("[Terminal] Timed out");
            }
        }
    }

    void ResetTerminal()
    {
        IsVisible.Value = false;
        IsOccupied.Value = false;
        OccupantClientId.Value = ulong.MaxValue;
    }

    // ── ServerRpc: player presses E near terminal ─────────────
    [Rpc(SendTo.Server)]
    public void InteractServerRpc(ulong callerClientId)
    {
        if (!IsVisible.Value || IsOccupied.Value) return;

        IsOccupied.Value = true;
        OccupantClientId.Value = callerClientId;

        Debug.Log($"[Terminal] Locked by client {callerClientId}");
        OpenTypingUIRpc(RpcTarget.Single(callerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void OpenTypingUIRpc(RpcParams rpcParams = default)
    {
        AccusationUI.Instance.Open(this);
    }

    // ── ServerRpc: player submits accusation ──────────────────
    [Rpc(SendTo.Server)]
    public void SubmitAccusationServerRpc(ulong accusedClientId, FixedString512Bytes accusationText)
    {
        if (OccupantClientId.Value == ulong.MaxValue) return;
        ulong accuserId = OccupantClientId.Value;

        // build context from accused player state
        BasePlayer accused = PlayerRegistry.Instance.GetPlayer(accusedClientId);
        string context = accused != null
            ? $"Accused: Player {accusedClientId}. " +
                $"Role: {accused.Role.Value}. " +
                $"Is stunned: {accused.IsStunned.Value}."
                : $"Accused: Player {accusedClientId}. State unknown.";

        StartCoroutine(GetVerdict(accuserId, accusedClientId, accusationText.ToString(), context));
    }

    IEnumerator GetVerdict(ulong accuserId, ulong accusedId, string accusation, string context)
    {
        bool guilty = false;
        yield return StartCoroutine(
            ClaudeService.Instance.GetJudgeVerdict(accusation, context,
                result => guilty = result));

        // broadcast verdict to all
        string msg = guilty
            ? $"GUILTY — Player {accusedId} stunned!"
            : $"BACKFIRE — Player {accuserId} stunned!";

        BroadcastVerdictRpc(guilty, msg);

        // stun the right player
        ulong stunTarget = guilty ? accusedId : accuserId;
        BasePlayer target = PlayerRegistry.Instance.GetPlayer(stunTarget);
        if (target != null)
            target.ApplyStunServerRpc(10f);

        ResetTerminal();
    }

    [Rpc(SendTo.Everyone)]
    void BroadcastVerdictRpc(bool guilty, FixedString128Bytes msg)
    {
        Debug.Log($"[Terminal] Verdict: {msg}");
        // TODO: hook to verdict UI banner
    }

    // ── Client: react to NetworkVariable changes ──────────────
    void OnVisibilityChanged(bool prev, bool curr)
    {
        if (visualRoot) visualRoot.SetActive(curr);
        if (!curr && interactPrompt) interactPrompt.SetActive(false);
        if (curr) Debug.Log("[Terminal] Visible to all clients");
    }

    void OnOccupiedChanged(bool prev, bool curr)
    {
        if (interactPrompt)
            interactPrompt.SetActive(IsVisible.Value && !curr);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsVisible.Value || IsOccupied.Value) return;
        var player = other.GetComponent<BasePlayer>();
        if (player != null && player.IsOwner)
            if (interactPrompt) interactPrompt.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<BasePlayer>();
        if (player != null && player.IsOwner)
            if (interactPrompt) interactPrompt.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        IsVisible.OnValueChanged -= OnVisibilityChanged;
        IsOccupied.OnValueChanged -= OnOccupiedChanged;
        if (_cycleRoutine != null) StopCoroutine(_cycleRoutine);
    }
}