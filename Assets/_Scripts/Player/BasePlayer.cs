
using Unity.Netcode;
using UnityEngine;

public enum PlayerRole { Worker, Saboteur }

public class BasePlayer : NetworkBehaviour
{
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

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        Debug.Log($"[BasePlayer] Spawned as: {Role.Value}");
    }

    // ── Update ────────────────────────────────────────────────
    protected virtual void Update()
    {
        // input — owner only
        if (IsOwner && !IsStunned.Value)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }
        else if (IsOwner && IsStunned.Value)
        {
            movement = Vector2.zero;
        }

        // stun countdown — server only
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
        Vector3 position = GameObject.FindGameObjectWithTag("Player").transform.position;
        Debug.Log($"Player position: {position}");
    }

    // Slow (called by Saboteur coffee ability)
    public void ApplySlow(float amount, float duration)
    {
        if (!IsServer) return;
        StartCoroutine(SlowCoroutine(amount, duration));
    }

    private System.Collections.IEnumerator SlowCoroutine(float amount, float duration)
    {
        speed *= (1f - amount);
        yield return new WaitForSeconds(duration);
        speed /= (1f - amount);
    }

    // RPCs
    [Rpc(SendTo.Server)]
    public void ApplyStunServerRpc(float duration)
    {
        if (IsStunned.Value) return;
        IsStunned.Value = true;
        _stunTimer = duration;
    }

    [Rpc(SendTo.Server)]
    public void SetRoleServerRpc(PlayerRole role)
    {
        Role.Value = role;
    }
}