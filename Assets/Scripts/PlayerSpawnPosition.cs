using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Spawn positions for teams.
/// </summary>
public class PlayerSpawnPosition : MonoBehaviour
{
    /// <summary>
    /// The team this spawn is for.
    /// </summary>
    [SerializeField]
    [Tooltip("The team this spawn is for.")]
    private Team team = Team.Red;
    
    /// <summary>
    /// Hold all spawns.
    /// </summary>
    private static readonly HashSet<PlayerSpawnPosition> SpawnPositions = new();

    private void Awake()
    {
        // Add this position.
        SpawnPositions.Add(this);
    }

    private void OnDestroy()
    {
        // Remove this position.
        SpawnPositions.Remove(this);
    }

    /// <summary>
    /// Spawn the player.
    /// </summary>
    /// <param name="eliminatedBy"></param>
    /// <returns>Nothing.</returns>
    public static IEnumerator SpawnPlayer(PlayerController eliminatedBy = null)
    {
        // If already respawning, no need to try and run this again.
        if (GameManager.localPlayer.respawning)
        {
            yield break;
        }
        
        // Set to respawning.
        GameManager.localPlayer.SetRespawningCmd(true);
        
        // Disable the player until done respawning.
        GameManager.localPlayer.CharacterController.enabled = false;
        GameManager.localPlayer.SetHealthCmd(0);
        GameManager.localPlayer.canMove = false;

        // Save the last alive position and the target to look at.
        GameManager.eliminatedPosition = GameManager.CameraPosition.position;
        GameManager.eliminatedTarget = eliminatedBy;

        // Get the team spawns.
        HashSet<PlayerSpawnPosition> positions = GameManager.Team switch
        {
            Team.None => new(),
            _ => SpawnPositions.Where(x => x.team == GameManager.Team).ToHashSet()
        };

        // Spawn at a team position if on a team and there are some, otherwise get a standard start position.
        Transform spawnTransform = positions.Any() ? positions.ElementAt(new System.Random().Next(positions.Count)).transform : NetworkManager.singleton.GetStartPosition();
        
        // Set the player to the spawn location.
        GameManager.localPlayer.transform.position = spawnTransform.position;
        GameManager.localPlayer.transform.rotation = Quaternion.Euler(0, spawnTransform.eulerAngles.y, 0);
        
        // Level out the vertical view.
        GameManager.localPlayer.rotationYLocal = 0;
        
        // If the player was eliminated by someone, watch them for a moment before respawning.
        // Otherwise, it was an initial spawn, so don't apply a delay.
        if (eliminatedBy != null)
        {
            yield return new WaitForSeconds(GameManager.RespawnDelay);
        }

        // Remove the player to spectate while dead.
        GameManager.eliminatedTarget = null;
        
        // Enable the player.
        GameManager.localPlayer.CharacterController.enabled = true;
        GameManager.localPlayer.SetHealthCmd(GameManager.Health);
        GameManager.localPlayer.canMove = true;

        // Flag that respawning has finished.
        GameManager.localPlayer.SetRespawningCmd(false);
    }
}