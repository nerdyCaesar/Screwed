using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class Saboteur : BasePlayer
{
    [Header("Router Unplug — AoE stun")]
    public float routerUnplugRadius = 4f;
    public float routerUnplugDuration = 5f;
    public float routerUnplugCooldown = 20f;
    private float _routerTimer = 0f;

    [Header("Spill Coffee — slow")]
    public float coffeeCooldown = 15f;
    public float coffeeSlowDuration = 6f;
    public float coffeeSlowAmount = 0.5f;
    private float _coffeeTimer = 0f;

    [Header("Rubber Duck — distract judge")]
    public float rubberDuckCooldown = 25f;
    public float rubberDuckDuration = 8f;
    private float _duckTimer = 0f;

    protected override void Update()
    {
        base.Update();
        if (!IsOwner) return;

        _routerTimer = Mathf.Max(0, _routerTimer - Time.deltaTime);
        // _coffeeTimer = Mathf.Max(0, _coffeeTimer - Time.deltaTime);
        // _duckTimer = Mathf.Max(0, _duckTimer - Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Q) && _routerTimer <= 0f)
            RouterUnplugServerRpc();

        // if (Input.GetKeyDown(KeyCode.E) && _coffeeTimer <= 0f)
        //     SpillCoffeeServerRpc();

        // if (Input.GetKeyDown(KeyCode.R) && _duckTimer <= 0f)
        //     RubberDuckServerRpc();
    }

    // ── Q: Router Unplug ──────────────────────────────────────
    [Rpc(SendTo.Server)]
    void RouterUnplugServerRpc()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, routerUnplugRadius, Physics2D.AllLayers);

        foreach (var hit in hits)
        {
            var worker = hit.GetComponent<Worker>();
            if (worker == null) continue;
            if (worker.TryAbsorbStun()) continue;
            worker.ApplyStunServerRpc(routerUnplugDuration);
        }

        GetComponent<PlayerState>().RecordAction("used router unplug");
        SyncCooldownClientRpc(routerUnplugCooldown, 0);
    }

    // // ── E: Spill Coffee ───────────────────────────────────────
    // [Rpc(SendTo.Server)]
    // void SpillCoffeeServerRpc()
    // {
    //     Worker target = FindNearestWorker();
    //     if (target == null) return;
    //     target.ApplySlow(coffeeSlowAmount, coffeeSlowDuration);
    //     GetComponent<PlayerState>().RecordAction("spilled coffee");
    //     SyncCooldownClientRpc(coffeeCooldown, 1);
    // }

    // // ── R: Rubber Duck ────────────────────────────────────────
    // [Rpc(SendTo.Server)]
    // void RubberDuckServerRpc()
    // {
    //     var judges = FindObjectsByType<JudgeNPC>(FindObjectsSortMode.None);
    //     JudgeNPC nearest = null;
    //     float bestDist = float.MaxValue;

    //     foreach (var j in judges)
    //     {
    //         float d = Vector2.Distance(transform.position, j.transform.position);
    //         if (d < bestDist) { bestDist = d; nearest = j; }
    //     }

    //     if (nearest != null)
    //         nearest.SetDistracted(rubberDuckDuration);

    //     GetComponent<PlayerState>().RecordAction("threw rubber duck");
    //     SyncCooldownClientRpc(rubberDuckCooldown, 2);
    // }

    // HELPERS
    Worker FindNearestWorker()
    {
        var workers = FindObjectsByType<Worker>(FindObjectsSortMode.None);
        Worker nearest = null;
        float bestDist = float.MaxValue;
        foreach (var w in workers)
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; nearest = w; }
        }
        return nearest;
    }

    [Rpc(SendTo.Owner)]
    void SyncCooldownClientRpc(float duration, int abilityIndex)
    {
        switch (abilityIndex)
        {
            case 0: _routerTimer = duration; break;
            case 1: _coffeeTimer = duration; break;
            case 2: _duckTimer = duration; break;
        }
    }
}