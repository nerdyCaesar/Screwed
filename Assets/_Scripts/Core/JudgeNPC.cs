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
        // The server is authoritative for NPC behavior.
        if (!IsServer) return;
        if (_isDistracted) return;

        if (_isChasing && _chaseTarget != null)
            ChasePlayer();
        else
            Patrol();

        ScanForPlayers();
    }

    // Move between waypoints in a loop while idle.
    void Patrol()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[_currentWaypoint];
        Vector2 dir = (target.position - transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        if (Vector2.Distance(transform.position, target.position) < waypointRadius)
            _currentWaypoint = (_currentWaypoint + 1) % waypoints.Length;
    }

    // Pursue the current target until they are stunned or lost.
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

        // Trigger stun once the judge is close enough.
        if (Vector2.Distance(transform.position,
                             _chaseTarget.transform.position) < 0.8f)
        {
            StunTarget(_chaseTarget);
            _isChasing = false;
            _chaseTarget = null;
        }
    }

    // Find an unstunned player in range and start chasing.
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

            // Any unstunned player in radius becomes a valid chase target.
            _isChasing = true;
            _chaseTarget = player;
            return;
        }
    }

    // Apply stun on the server and notify clients for feedback UI.
    void StunTarget(BasePlayer player)
    {
        player.ApplyStunServerRpc(stunDuration);
        NotifyStunClientRpc(player.OwnerClientId);
    }

    [Rpc(SendTo.Everyone)]
    void NotifyStunClientRpc(ulong clientId)
    {
        // TODO: Show roast bubble and connect this event to CodeReviewManager.
    }

    // Temporarily pause judge behavior while distracted.
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
        yield return new WaitForSeconds(duration);
        _isDistracted = false;
    }

    // Draw detection range in the editor when this NPC is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}