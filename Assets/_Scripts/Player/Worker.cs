using Unity.Netcode;
using UnityEngine;

public class Worker : BasePlayer
{
    [Header("Coding")]
    public float codingSpeed = 2f;
    private bool _isAtDesk = false;

    protected override void Update()
    {
        // Reuse base movement and interaction input handling.
        base.Update();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!IsServer) return;
        if (_isAtDesk && !IsStunned.Value && !GameManager.Instance.ProgressPaused)
            GameManager.Instance.AddProgress(codingSpeed * Time.fixedDeltaTime);
    }

    public void SetAtDesk(bool value)
    {
        _isAtDesk = value;
        if (!IsServer)
            SetAtDeskServerRpc(value);
    }

    [Rpc(SendTo.Server)]
    void SetAtDeskServerRpc(bool value) => _isAtDesk = value;
}