using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class JudgeNPC : NetworkBehaviour
{
    [Header("Patrol")]
    [SerializeField] Transform[] waypoints;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float waypointRadius = 0.3f;
    private int _currentWaypoint = 0;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 3f;
    [SerializeField] float chaseSpeed = 4f;

    [Header("Stun")]
    [SerializeField] float stunDuration = 10f;

    private bool _isDistracted = false;
    private bool _isChasing = false;
    private BasePlayer _chaseTarget = null;
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // only host controls NPCs
        if (!IsServer) return;
        if (_isDistracted) return;

        if (_isChasing && _chaseTarget != null)
            ChasePlayer();
        else
            Patrol();

        ScanForPlayers();
    }

    // ── Patrol between waypoints ──────────────────────────────
    void Patrol()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[_currentWaypoint];
        Vector2 dir = (target.position - transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        if (Vector2.Distance(transform.position, target.position) < waypointRadius)
            _currentWaypoint = (_currentWaypoint + 1) % waypoints.Length;
    }

    // ── Chase detected player ─────────────────────────────────
    void ChasePlayer()
    {
        if (_chaseTarget == null || _chaseTarget.IsStunned.Value)
        {
            _isChasing = false;
            _chaseTarget = null;
            return;
        }

        Vector2 dir = ((Vector2)_chaseTarget.transform.position
                       - _rb.position).normalized;
        _rb.MovePosition(_rb.position + dir * chaseSpeed * Time.fixedDeltaTime);

        // close enough to stun
        if (Vector2.Distance(transform.position,
                             _chaseTarget.transform.position) < 0.8f)
        {
            StunTarget(_chaseTarget);
            _isChasing = false;
            _chaseTarget = null;
        }
    }

    // ── Scan nearby players ───────────────────────────────────
    void ScanForPlayers()
    {
        if (_isChasing) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, detectionRadius);

        foreach (var hit in hits)
        {
            var player = hit.GetComponent<BasePlayer>();
            if (player == null) continue;
            if (player.IsStunned.Value) continue;

            // detect any player within radius — no velocity check needed
            _isChasing = true;
            _chaseTarget = player;
            Debug.Log($"[Judge] Detected player {player.OwnerClientId}");
            return;
        }
    }
    // ── Stun the caught player ────────────────────────────────
    void StunTarget(BasePlayer player)
    {
        player.ApplyStunServerRpc(stunDuration);
        NotifyStunClientRpc(player.OwnerClientId);
        Debug.Log($"[Judge] Stunned player {player.OwnerClientId}");
    }

    [Rpc(SendTo.Everyone)]
    void NotifyStunClientRpc(ulong clientId)
    {
        Debug.Log($"[Judge] Player {clientId} got code reviewed!");
        // TODO: hook to roast bubble + CodeReviewManager
    }

    // ── Rubber duck distraction ───────────────────────────────
    public void SetDistracted(float duration)
    {
        if (!IsServer) return;
        StartCoroutine(DistractCoroutine(duration));
    }

    IEnumerator DistractCoroutine(float duration)
    {
        _isDistracted = true;
        _isChasing = false;
        _chaseTarget = null;
        Debug.Log($"[Judge] Distracted for {duration}s");
        yield return new WaitForSeconds(duration);
        _isDistracted = false;
        Debug.Log("[Judge] Back on duty");
    }

    // ── Debug gizmo — see detection radius in scene view ─────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}