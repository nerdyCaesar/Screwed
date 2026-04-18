using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance;

    // server-side lookup: clientId → player object
    private Dictionary<ulong, BasePlayer> _players = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[PlayerRegistry] Client connected: {clientId}");
    }

    void OnClientDisconnected(ulong clientId)
    {
        _players.Remove(clientId);
        Debug.Log($"[PlayerRegistry] Client removed: {clientId}");
    }

    // called by each player on spawn
    public void Register(ulong clientId, BasePlayer player)
    {
        if (!_players.ContainsKey(clientId))
            _players.Add(clientId, player);
    }

    public BasePlayer GetPlayer(ulong clientId)
    {
        _players.TryGetValue(clientId, out var player);
        return player;
    }

    public List<BasePlayer> GetAllPlayers()
    {
        return new List<BasePlayer>(_players.Values);
    }

    public List<Worker> GetAllWorkers()
    {
        var workers = new List<Worker>();
        foreach (var p in _players.Values)
            if (p is Worker w) workers.Add(w);
        return workers;
    }

    public List<Saboteur> GetAllSaboteurs()
    {
        var saboteurs = new List<Saboteur>();
        foreach (var p in _players.Values)
            if (p is Saboteur s) saboteurs.Add(s);
        return saboteurs;
    }

    public BasePlayer GetNearestOtherPlayer(Vector2 position, ulong excludeClientId)
    {
        BasePlayer nearest = null;
        float bestDist = float.MaxValue;
        foreach (var p in _players.Values)
        {
            if (p.OwnerClientId == excludeClientId) continue;
            float d = Vector2.Distance(position, p.transform.position);
            if (d < bestDist) { bestDist = d; nearest = p; }
        }
        return nearest;
    }
}