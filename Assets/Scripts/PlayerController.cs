using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The controller for the characters.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformReliable))]
public class PlayerController : NetworkBehaviour
{
    /// <summary>
    /// The view rotation controlled on the local player.
    /// </summary>
    [NonSerialized]
    public float rotationYLocal;

    /// <summary>
    /// The health of the player.
    /// </summary>
    [NonSerialized]
    [SyncVar(hook = nameof(OnHealthChange))]
    public int health;

    /// <summary>
    /// The state of the player.
    /// </summary>
    [NonSerialized]
    [SyncVar(hook = nameof(OnGameStateChange))]
    public GameState gameState = GameState.Playing;

    /// <summary>
    /// If currently respawning.
    /// </summary>
    [NonSerialized]
    [SyncVar]
    public bool respawning;

    /// <summary>
    /// If this player can move.
    /// </summary>
    [NonSerialized]
    [SyncVar]
    public bool canMove;

    /// <summary>
    /// If this player is ready to start the match.
    /// </summary>
    [field: SyncVar]
    public bool Ready { get; private set; }

    /// <summary>
    /// The team this player is on.
    /// </summary>
    [field: SyncVar(hook = nameof(OnTeamChange))]
    public Team Team { get; private set; } = Team.None;

    /// <summary>
    /// The name of the player.
    /// </summary>
    [field: SyncVar]
    public string PlayerName { get; private set; }

    /// <summary>
    /// Where the camera will be positioned.
    /// </summary>
    public Transform CameraPosition => cameraPosition;

    /// <summary>
    /// The controller.
    /// </summary>
    public CharacterController CharacterController => _characterController != null ? _characterController : GetComponent<CharacterController>();
    
    /// <summary>
    /// Where the camera will be positioned.
    /// </summary>
    [SerializeField]
    [Tooltip("Where the camera will be positioned.")]
    private Transform cameraPosition;

    /// <summary>
    /// The head of the player visuals.
    /// </summary>
    [SerializeField]
    [Tooltip("The head of the player visuals.")]
    private Transform headPosition;

    /// <summary>
    /// All meshes that change color based on the team.
    /// </summary>
    [SerializeField]
    [Tooltip("All meshes that change color based on the team.")]
    private MeshRenderer[] teamColorVisuals;

    /// <summary>
    /// All other visuals that need to be hidden when the player dies.
    /// </summary>
    [SerializeField]
    [Tooltip("All other visuals that need to be hidden when the player dies.")]
    private MeshRenderer[] otherVisuals;
    
    /// <summary>
    /// Sync the view to remote players.
    /// </summary>
    [SyncVar]
    private float _rotationYRemote;

    /// <summary>
    /// The controller.
    /// </summary>
    private CharacterController _characterController;

    /// <summary>
    /// The main capsule collider for remote players.
    /// </summary>
    private CapsuleCollider _capsuleCollider;

    /// <summary>
    /// The colliders in the children of this object for hit boxes.
    /// </summary>
    private Collider[] _childColliders;

    /// <summary>
    /// The current falling velocity.
    /// </summary>
    private float _velocityY;
    
    /// <summary>
    /// If falling or not.
    /// </summary>
    private bool _airborne;

    /// <summary>
    /// Receive a message.
    /// </summary>
    /// <param name="text">The message.</param>
    [Command(requiresAuthority = false)]
    public void MessageCmd(string text)
    {
        ReceiveMessageRpc(text);
    }

    /// <summary>
    /// Receive a message.
    /// </summary>
    /// <param name="text">The message.</param>
    [ClientRpc]
    private void ReceiveMessageRpc(string text)
    {
        GameManager.AddMessage(text);
    }

    /// <summary>
    /// Set the health of this player.
    /// </summary>
    /// <param name="value">The value to set it to.</param>
    [Command(requiresAuthority = false)]
    public void SetHealthCmd(int value)
    {
        health = Mathf.Clamp(value, 0, GameManager.Health);
    }
    
    /// <summary>
    /// Set that this player is respawning or not.
    /// </summary>
    /// <param name="value">If it is respawning or not.</param>
    [Command]
    public void SetRespawningCmd(bool value)
    {
        respawning = value;
    }

    /// <summary>
    /// Set that this player is ready or not.
    /// </summary>
    /// <param name="ready">The value to set being ready to.</param>
    [Command]
    public void SetReadyCmd(bool ready)
    {
        Ready = ready;
    }

    /// <summary>
    /// Set the team on the server.
    /// </summary>
    /// <param name="team">The team to switch to.</param>
    [Command]
    public void SetTeamCmd(Team team)
    {
        // If not on a team, change it to the team with less players.
        if (team == Team.None)
        {
            team = GameManager.players.Count(x => x.Team == Team.Red) <= GameManager.players.Count(x => x.Team == Team.Blue) ? Team.Red : Team.Blue;
        }
        
        Team = team;
    }

    /// <summary>
    /// Whenever the team is changed, update a copy of the value in the game manager.
    /// </summary>
    /// <param name="_">Old value which is required but not used.</param>
    /// <param name="__">New value which is required but not used.</param>
    private void OnTeamChange(Team _, Team __)
    {
        if (isLocalPlayer)
        {
            GameManager.SetTeam();
        }
        
        // Set the materials.
        UpdateMaterials();
    }

    /// <summary>
    /// When the game state changes.
    /// </summary>
    /// <param name="_">Old value which is required but not used.</param>
    /// <param name="newGameState">New game state.</param>
    private void OnGameStateChange(GameState _, GameState newGameState)
    {
        foreach (PlayerController playerController in FindObjectsOfType<PlayerController>())
        {
            playerController.gameState = newGameState;
        }
    }

    /// <summary>
    /// Called when the health changes.
    /// </summary>
    /// <param name="_">Old value which is required but not used.</param>
    /// <param name="newHealth">The new health value.</param>
    private void OnHealthChange(int _, int newHealth)
    {
        // Determine if the player is alive or not.
        bool alive = newHealth > 0;
        
        // Loop through all colliders in the children and set if they are ready.
        foreach (Collider col in _childColliders ?? GetComponentsInChildren<Collider>())
        {
            col.enabled = alive;
        }
        
        // On the local player, show only shadows.
        ShadowCastingMode shadowCastingMode = alive
            ? isLocalPlayer ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On
            : ShadowCastingMode.Off;
        
        // Configure the mesh renderers for visuals.
        foreach (MeshRenderer meshRenderer in teamColorVisuals)
        {
            meshRenderer.shadowCastingMode = shadowCastingMode;
            meshRenderer.enabled = alive;
        }

        // Configure the remaining mesh renderers.
        foreach (MeshRenderer meshRenderer in  otherVisuals)
        {
            meshRenderer.shadowCastingMode = shadowCastingMode;
            meshRenderer.enabled = alive;
        }
    }

    private void Awake()
    {
        // Store a reference to this player.
        GameManager.players.Add(this);
        
        // If not in the lobby and not on a team, leave the match.
        if (!GameManager.IsLobby && GameManager.Team == Team.None)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    private void OnDestroy()
    {
        // Remove the reference to this player.
        GameManager.players.Remove(this);
    }

    private void Start()
    {
        // Get all components.
        _characterController = GetComponent<CharacterController>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _childColliders = GetComponentsInChildren<Collider>().ToArray();
        
        // Adjust the size so it is not floating because of the skin width.
        _characterController.height -= _characterController.skinWidth * 2;
        _characterController.radius -= _characterController.skinWidth;

        // The collider is only active for remote connections to hit against.
        _capsuleCollider.enabled = !isLocalPlayer;

        // The character controller is only active for the controlling player.
        _characterController.enabled = isLocalPlayer;
        
        // Ensure proper materials are active.
        UpdateMaterials();
    }

    public override void OnStartLocalPlayer()
    {
        // Keep a reference to the local player.
        GameManager.localPlayer = this;
        
        // Apply parameters.
        SetNameCmd(GameManager.PlayerName);
        SetTeamCmd(GameManager.Team);
        GetGameState();

        // Set to the local player layer to avoid any collisions.
        int layer = LayerMask.NameToLayer("LocalPlayer");
        foreach (Transform tr in GetComponentsInChildren<Transform>())
        {
            tr.gameObject.layer = layer;
        }

        // Get an initial spawn for the player.
        StartCoroutine(PlayerSpawnPosition.SpawnPlayer());

        // If the server and not in the lobby, begin the match start countdown.
        if (isServer && !GameManager.IsLobby)
        {
            StartCountdownCmd();
        }
    }

    /// <summary>
    /// Begin the match start countdown.
    /// </summary>
    [Command]
    private void StartCountdownCmd()
    {
        StartCoroutine(StartCountdown());
    }
    
    /// <summary>
    /// Wait for a time before allowing players to move.
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartCountdown()
    {
        gameState = GameState.Starting;
        yield return new WaitForSeconds(GameManager.SceneSwitchDelay);
        gameState = GameState.Playing;
    }

    private void Update()
    {
        // On remote clients, simply sync certain values.
        if (!isLocalPlayer)
        {
            // Ensure the character controller is not enabled.
            _characterController.enabled = false;

            // Aim the head where the player is looking.
            headPosition.localRotation = Quaternion.Euler(Mathf.Clamp(_rotationYRemote, -45, 45), 0, 0);
            return;
        }
        
        // On the local client, move.
        Movement();
    }

    /// <summary>
    /// Handle movement logic.
    /// </summary>
    private void Movement()
    {
        // Can only move if the controller is enabled.
        if (!_characterController.enabled)
        {
            return;
        }
        
        // Rotate first.
        transform.Rotate(0, GameManager.Look.x * GameManager.Sensitivity * Time.deltaTime, 0);

        // Vertical camera movement.
        rotationYLocal = Mathf.Clamp(rotationYLocal + -GameManager.Look.y * GameManager.Sensitivity * Time.deltaTime, -90, 90);

        // Sync vertical view across the network.
        if (NetworkClient.ready)
        {
            UpdateLookRotationCmd(rotationYLocal);
        }
        
        // Update the local vertical rotation.
        cameraPosition.transform.localRotation = Quaternion.Euler(rotationYLocal, 0, 0);
        
        // If on the ground.
        if (_characterController.isGrounded)
        {
            // Zero out any falling velocity.
            _velocityY = 0;
            
            // If requested to jump and are able to, jump.
            if (canMove && gameState == GameState.Playing && GameManager.Jump)
            {
                _airborne = true;
                _velocityY += Mathf.Sqrt(-GameManager.JumpForce * Physics.gravity.y);
            }
            // Otherwise, not airborne.
            else
            {
                _airborne = false;
            }
        }
        
        // If not airborne, perform additional checks to confirm this.
        if (!_airborne)
        {
            // Raycast down to see if we can stay on the ground to deal with slopes given the character is not set to be airborne.
            if (Physics.Raycast(transform.position + new Vector3(0, _characterController.center.y, 0), Vector3.down, GameManager.GroundedDistance))
            {
                // Snap down to the ground.
                _velocityY = Physics.gravity.y;
            }
            // Otherwise, something such as walking off a ledge has happened, so the character is airborne.
            else
            {
                _airborne = true;
            }
        }

        // Add gravity.
        _velocityY += Physics.gravity.y * Time.deltaTime;
        
        // Apply movement.
        Transform tr = transform;
        Vector3 targetVelocity = canMove && gameState == GameState.Playing ? (GameManager.Move.y * tr.forward + GameManager.Move.x * tr.right) * GameManager.Speed : Vector3.zero;
        targetVelocity.y = _velocityY;
        _characterController.Move(targetVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Update vertical look rotation on the server.
    /// </summary>
    /// <param name="rotationY">The rotation to set.</param>
    [Command]
    private void UpdateLookRotationCmd(float rotationY)
    {
        _rotationYRemote = rotationY;
    }

    /// <summary>
    /// Update the name on the server.
    /// </summary>
    /// <param name="playerName">The name.</param>
    [Command]
    private void SetNameCmd(string playerName)
    {
        PlayerName = playerName;
    }

    /// <summary>
    /// Update materials.
    /// </summary>
    private void UpdateMaterials()
    {
        // Get the material for the team.
        Material material = Team switch
        {
            Team.Red => GameManager.RedTeamMaterial,
            Team.Blue => GameManager.BlueTeamMaterial,
            _ => GameManager.NoneMaterial
        };

        // Apply team visuals.
        foreach (MeshRenderer meshRenderer in teamColorVisuals)
        {
            meshRenderer.material = material;
        }
    }

    /// <summary>
    /// Get the current state of the game by seeing what the majority between all players is.
    /// </summary>
    private void GetGameState()
    {
        // If on the server, not needed.
        if (isServer)
        {
            return;
        }

        // Count the state of each player.
        int starting = 0;
        int playing = 0;
        int ending = 0;

        // Loop through all players except the local player.
        foreach (PlayerController playerController in GameManager.players.Where(playerController => !playerController.isLocalPlayer))
        {
            // Add the given state.
            switch (playerController.gameState)
            {
                case GameState.Starting:
                    starting++;
                    break;
                case GameState.Playing:
                    playing++;
                    break;
                case GameState.Ending:
                default:
                    ending++;
                    break;
            }
        }

        // Choose the greatest state.
        if (starting >= playing && starting >= ending)
        {
            gameState = GameState.Starting;
            return;
        }

        if (ending > starting && ending >= playing)
        {
            gameState = GameState.Ending;
            return;
        }

        gameState = GameState.Playing;
    }
}