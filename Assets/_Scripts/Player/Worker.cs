using Unity.Netcode;
using UnityEngine;

public class Worker : BasePlayer
{
    [Header("Coding")]
    public float codingSpeed = 2f;
    private bool _isAtDesk = false;

    [Header("Shield")]
    public float shieldCooldown = 30f;
    private float _shieldTimer = 0f;
    private bool _shieldActive = false;

    [Header("Sprint")]
    public float sprintCooldown = 12f;
    public float sprintDuration = 3f;
    public float sprintMultiplier = 1.8f;
    private float _sprintCooldownTimer = 0f;
    private float _sprintActiveTimer = 0f;
    private bool _isSprinting = false;

    protected override void Update()
    {
        base.Update();
        if (!IsOwner) return;

        _shieldTimer = Mathf.Max(0, _shieldTimer - Time.deltaTime);
        _sprintCooldownTimer = Mathf.Max(0, _sprintCooldownTimer - Time.deltaTime);

        if (_isSprinting)
        {
            _sprintActiveTimer -= Time.deltaTime;
            if (_sprintActiveTimer <= 0f)
                StopSprintServerRpc();
        }

        if (Input.GetKeyDown(KeyCode.Q) && _shieldTimer <= 0f)
            ActivateShieldServerRpc();

        if (Input.GetKeyDown(KeyCode.E) && _sprintCooldownTimer <= 0f && !_isSprinting)
            ActivateSprintServerRpc();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!IsServer) return;
        if (_isAtDesk && !IsStunned.Value)
            GameManager.Instance.AddProgress(codingSpeed * Time.fixedDeltaTime);
    }

    // ── called by DeskInteractable when worker enters/exits ───
    public void SetAtDesk(bool value)
    {
        _isAtDesk = value;
        if (IsServer) return;
        SetAtDeskServerRpc(value);
    }

    [Rpc(SendTo.Server)]
    void SetAtDeskServerRpc(bool value) => _isAtDesk = value;

    // ── Shield ────────────────────────────────────────────────
    [Rpc(SendTo.Server)]
    void ActivateShieldServerRpc()
    {
        _shieldActive = true;
        SetShieldClientRpc(true);
    }

    [Rpc(SendTo.Everyone)]
    void SetShieldClientRpc(bool active)
    {
        _shieldActive = active;
        // TODO: toggle shield VFX
    }

    public bool TryAbsorbStun()
    {
        if (!_shieldActive) return false;
        _shieldActive = false;
        _shieldTimer = shieldCooldown;
        SetShieldClientRpc(false);
        return true;
    }

    // ── Sprint ────────────────────────────────────────────────
    [Rpc(SendTo.Server)]
    void ActivateSprintServerRpc()
    {
        speed *= sprintMultiplier;
        _isSprinting = true;
        _sprintActiveTimer = sprintDuration;
        SetSprintClientRpc(true);
    }

    [Rpc(SendTo.Server)]
    void StopSprintServerRpc()
    {
        speed /= sprintMultiplier;
        _isSprinting = false;
        SetSprintClientRpc(false);
    }

    [Rpc(SendTo.Everyone)]
    void SetSprintClientRpc(bool active)
    {
        _isSprinting = active;
        if (IsOwner)
            _sprintCooldownTimer = active ? 0f : sprintCooldown;
        // TODO: sprint VFX
    }
}