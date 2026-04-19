using Unity.Netcode;
using UnityEngine;

public class Saboteur : BasePlayer
{
    [Header("Router Unplug")]
    public float routerUnplugRadius = 4f;
    public float routerUnplugDuration = 5f;
    public float routerUnplugCooldown = 20f;
    private float _routerTimer = 0f;

    protected override void Update()
    {
        base.Update();
        if (!IsOwner) return;

        _routerTimer = Mathf.Max(0, _routerTimer - Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Q) && _routerTimer <= 0f)
            RouterUnplugServerRpc();
    }

    [Rpc(SendTo.Server)]
    void RouterUnplugServerRpc()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, routerUnplugRadius, Physics2D.AllLayers);

        foreach (var hit in hits)
        {
            var worker = hit.GetComponent<Worker>();
            if (worker == null) continue;
            if (worker.TryAbsorbStun()) continue;
            worker.ApplyStunServerRpc(routerUnplugDuration);
        }

        GameManager.Instance.PauseProgress(routerUnplugDuration);
        SyncCooldownClientRpc(routerUnplugCooldown);
    }

    [Rpc(SendTo.Owner)]
    void SyncCooldownClientRpc(float duration)
    {
        _routerTimer = duration;
    }
}