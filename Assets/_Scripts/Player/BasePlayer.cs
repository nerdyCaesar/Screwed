using Unity.Netcode;
using UnityEngine;
using System.Collections;

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
        PlayerRegistry.Instance.Register(OwnerClientId, this);

        Role.OnValueChanged += OnRoleChanged;

        if (IsServer)
        {
            PlayerRole role = OwnerClientId == 0 ? PlayerRole.Saboteur : PlayerRole.Worker;
            Role.Value = role;
            Debug.Log($"[BasePlayer] Client {OwnerClientId} assigned {role}");
        }

        // apply color immediately and after sync
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

    void OnRoleChanged(PlayerRole prev, PlayerRole curr)
    {
        UpdateSprite(curr);
    }

    void UpdateSprite(PlayerRole role)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.color = role == PlayerRole.Saboteur
            ? new Color(0.94f, 0.33f, 0.31f)  // red
            : new Color(0.31f, 0.76f, 0.97f); // blue
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
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f);
                foreach (var hit in hits)
                {
                    var terminal = hit.GetComponent<TattletaleTerminal>();
                    if (terminal != null
                        && terminal.gameObject.activeSelf
                        && terminal.IsSpawned          // ← fix NullRef
                        && !terminal.IsOccupied.Value)
                    {
                        Debug.Log("[Player] Terminal found — sending interact");
                        terminal.InteractServerRpc(NetworkManager.Singleton.LocalClientId);
                        break;
                    }
                }
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