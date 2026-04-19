using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TattletaleTerminal : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] GameObject interactPrompt;

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

    private bool _isProcessing = false;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[Terminal] OnNetworkSpawn — IsServer:{IsServer}");
        IsOccupied.OnValueChanged += OnOccupiedChanged;
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    void OnOccupiedChanged(bool prev, bool curr)
    {
        if (interactPrompt)
            interactPrompt.SetActive(!curr && gameObject.activeSelf);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (IsOccupied.Value) return;
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

    [Rpc(SendTo.Server)]
    public void InteractServerRpc(ulong callerClientId)
    {
        if (_isProcessing || IsOccupied.Value) return;
        _isProcessing = true;
        IsOccupied.Value = true;
        OccupantClientId.Value = callerClientId;
        Debug.Log($"[Terminal] Locked by client {callerClientId}");

        // tell everyone — the right client opens UI based on clientId
        NotifyInteractRpc(callerClientId);
    }

    [Rpc(SendTo.Everyone)]
    void NotifyInteractRpc(ulong clientId)
    {
        // only the occupant opens the UI
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.Log("[Terminal] Opening AccusationUI");
            AccusationUI.Instance.Open(this);
        }
    }

    [Rpc(SendTo.Server)]
    public void SubmitAccusationServerRpc(ulong accusedClientId,
                                          FixedString512Bytes accusationText)
    {
        if (OccupantClientId.Value == ulong.MaxValue) return;
        ulong accuserId = OccupantClientId.Value;

        BasePlayer accused = PlayerRegistry.Instance.GetPlayer(accusedClientId);
        string context = accused != null
            ? $"Accused: Player {accusedClientId}. Role: {accused.Role.Value}. Stunned: {accused.IsStunned.Value}."
            : $"Accused: Player {accusedClientId}. State unknown.";

        StartCoroutine(GetVerdict(accuserId, accusedClientId,
                                  accusationText.ToString(), context));
    }

    IEnumerator GetVerdict(ulong accuserId, ulong accusedId,
                           string accusation, string context)
    {
        bool guilty = false;
        yield return StartCoroutine(
            ClaudeService.Instance.GetJudgeVerdict(accusation, context,
                result => guilty = result));

        string msg = guilty
            ? $"GUILTY — Player {accusedId} stunned!"
            : $"BACKFIRE — Player {accuserId} stunned!";

        BroadcastVerdictRpc(guilty, msg);

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
    }

    void ResetTerminal()
    {
        _isProcessing = false;
        IsOccupied.Value = false;
        OccupantClientId.Value = ulong.MaxValue;
    }

    public override void OnNetworkDespawn()
    {
        IsOccupied.OnValueChanged -= OnOccupiedChanged;
    }
}