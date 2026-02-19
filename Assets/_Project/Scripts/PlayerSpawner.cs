using Fusion;
using Network;
using UnityEngine;

/// <summary>
/// Spawns the local player, assigns input ownership,
/// and spawns a personal tower for each player.
/// Clean, safe, production-ready.
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    private static bool localPlayerSpawned = false;

    [Header("Player Settings")]
    public Vector3 hostSpawnPos = new Vector3(-4, 0, 0);
    public Vector3 clientSpawnPos = new Vector3(4, 0, 0);

    public override void Spawned()
    {
        Debug.Log($"[PlayerSpawner] Spawned → InputAuthority = {Object.InputAuthority}");

        // Only run this for the player that belongs to THIS client
        if (!Object.HasInputAuthority)
            return;

        // Prevent double spawn (rare Fusion bug on scene reload)
        if (localPlayerSpawned)
            return;

        Runner.SetPlayerObject(Runner.LocalPlayer, this.Object);
        localPlayerSpawned = true;
       // SpawnPlayerTower();
    }

    /// <summary>
    /// Spawns a personal tower for each player.
    /// </summary>
    void SpawnPlayerTower()
    {
        Debug.Log("[PlayerSpawner] Trying to spawn tower...");

        var manager = MultiplayerManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[PlayerSpawner] ERROR: No MultiplayerManager found!");
            return;
        }

        NetworkObject towerPrefab = manager.towerPrefab;

        if (towerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] ERROR: towerPrefab is NULL in MultiplayerManager!");
            return;
        }

        // Host left, client right
        Vector3 spawnPos = Runner.IsServer ? hostSpawnPos : clientSpawnPos;

        Debug.Log("[PlayerSpawner] Spawning tower at: " + spawnPos);

        var tower = Runner.Spawn(
            towerPrefab,
            spawnPos,
            Quaternion.identity,
            Object.InputAuthority
        );

        if (tower == null)
            Debug.LogError("[PlayerSpawner] ERROR: Runner failed to spawn tower!");
        else
            Debug.Log("[PlayerSpawner] SUCCESS tower spawned!");
    }

}
