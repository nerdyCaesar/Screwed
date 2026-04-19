using Unity.Netcode;
using UnityEngine;
using System.Collections;

public enum PlayerRole { Worker, Saboteur }

public class BasePlayer : NetworkBehaviour
{
    private float _abilityCooldownTimer = 0f;
    private const float ABILITY_COOLDOWN = 20f; // 20 seconds for both abilities
    [Header("Movement")]
    public float speed = 3f;
    public Rigidbody2D rb;
    private Vector2 movement;

    [Header("Role")]
    public NetworkVariable<PlayerRole> Role = new(
        PlayerRole.Worker,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Stun")]
    public NetworkVariable<bool> IsStunned = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private float _stunTimer = 0f;
    protected bool _shieldActive = false;


    public bool TryAbsorbStun()
    {
        if (!_shieldActive) return false;
        _shieldActive = false;
        return true;
    }
    public override void OnNetworkSpawn()
    {
        PlayerRegistry.Instance.Register(OwnerClientId, this);
        Role.OnValueChanged += OnRoleChanged;

        if (IsServer)
        {
            int count = PlayerRegistry.Instance.GetAllPlayers().Count;
            PlayerRole role = count % 2 == 0 ? PlayerRole.Saboteur : PlayerRole.Worker;
            Role.Value = role;
            Debug.Log($"[BasePlayer] Client {OwnerClientId} assigned {role}");
        }

        StartCoroutine(ApplyColorDelayed());

        if (!IsOwner) return;
        StartCoroutine(ShowRoleSplash());
    }

    IEnumerator ApplyColorDelayed()
    {
        yield return new WaitForSeconds(0.3f);
        UpdateSprite(Role.Value);
    }

    IEnumerator ShowRoleSplash()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"YOU ARE A {Role.Value.ToString().ToUpper()}");
    }

    void OnRoleChanged(PlayerRole prev, PlayerRole curr) => UpdateSprite(curr);

    void UpdateSprite(PlayerRole role)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.color = role == PlayerRole.Saboteur
            ? new Color(0.94f, 0.33f, 0.31f)
            : new Color(0.31f, 0.76f, 0.97f);
    }

    [Rpc(SendTo.Everyone)]
    void ShakeRpc()
    {
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake();
    }

    protected virtual void Update()
    {
        if (IsOwner && !IsStunned.Value)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[Player] E pressed");
                var t = FindFirstObjectByType<TattletaleTerminal>();
                if (t != null && t.IsSpawned && !t.IsOccupied.Value)
                {
                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.enabled)
                    {
                        Debug.Log("[Player] Interacting with terminal");
                        t.InteractServerRpc(NetworkManager.Singleton.LocalClientId);
                    }
                    else Debug.Log("[Player] Terminal not visible yet");
                }
                else Debug.Log("[Player] No terminal or occupied");
            }

            // cooldown countdown
            if (_abilityCooldownTimer > 0f)
                _abilityCooldownTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Q) && _abilityCooldownTimer <= 0f)
            {
                if (Role.Value == PlayerRole.Saboteur)
                    RouterUnplugServerRpc();
                else
                    ActivateShieldServerRpc();

                _abilityCooldownTimer = ABILITY_COOLDOWN;
            }


        }
        else if (IsOwner && IsStunned.Value)
        {
            movement = Vector2.zero;
        }

        if (IsServer && IsStunned.Value)
        {
            _stunTimer -= Time.deltaTime;
            if (_stunTimer <= 0f)
            {
                IsStunned.Value = false;
                _stunTimer = 0f;
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!IsOwner) return;
        if (IsStunned.Value) return;
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    [Rpc(SendTo.Server)]
    void RouterUnplugServerRpc()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 4f);
        foreach (var hit in hits)
        {
            var player = hit.GetComponent<BasePlayer>();
            if (player == null || player.Role.Value != PlayerRole.Worker) continue;
            player.ApplyStunServerRpc(5f);
        }
        GameManager.Instance.PauseProgress(5f);
        Debug.Log("[Ability] Router Unplug!");
        ShakeRpc(); // bigger shake for router unplug
        Debug.Log("[Ability] Router Unplug!");
    }

    [Rpc(SendTo.Server)]
    void ActivateShieldServerRpc()
    {
        _shieldActive = true;
        StartCoroutine(ShieldCoroutine());
        ShakeRpc();
        Debug.Log("[Ability] Shield active!");
    }

    IEnumerator ShieldCoroutine()
    {
        yield return new WaitForSeconds(5f);
        _shieldActive = false;
    }

    public void ApplySlow(float amount, float duration)
    {
        if (!IsServer) return;
        StartCoroutine(SlowCoroutine(amount, duration));
    }

    private IEnumerator SlowCoroutine(float amount, float duration)
    {
        speed *= (1f - amount);
        yield return new WaitForSeconds(duration);
        speed /= (1f - amount);
    }

    [Rpc(SendTo.Server)]
    public void ApplyStunServerRpc(float duration)
    {
        if (IsStunned.Value) return;
        if (_shieldActive) { _shieldActive = false; return; }
        IsStunned.Value = true;
        _stunTimer = duration;
    }

    [Rpc(SendTo.Server)]
    public void SetRoleServerRpc(PlayerRole role)
    {
        Role.Value = role;
    }

    public override void OnNetworkDespawn()
    {
        Role.OnValueChanged -= OnRoleChanged;
    }
}