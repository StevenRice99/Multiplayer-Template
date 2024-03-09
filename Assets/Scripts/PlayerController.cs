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
    private float _rotationYLocal;

    /// <summary>
    /// If this player is ready to start the match.
    /// </summary>
    [field: SyncVar]
    public bool Ready { get; private set; }

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
    /// Sync the view to remote players.
    /// </summary>
    [SyncVar]
    private float _rotationYRemote;

    /// <summary>
    /// The controller for the local player.
    /// </summary>
    private CharacterController _characterController;

    /// <summary>
    /// The main capsule collider for remote players.
    /// </summary>
    private CapsuleCollider _capsuleCollider;

    /// <summary>
    /// The current falling velocity.
    /// </summary>
    private float _velocityY;
    
    /// <summary>
    /// If falling or not.
    /// </summary>
    private bool _airborne;

    /// <summary>
    /// Set that this player is ready or not.
    /// </summary>
    /// <param name="ready">The value to set being ready to.</param>
    [Command]
    public void SetReadyCmd(bool ready)
    {
        Ready = ready;
    }

    private void Awake()
    {
        // Store a reference to this player.
        GameManager.players.Add(this);
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
        
        // Adjust the size so it is not floating because of the skin width.
        _characterController.height -= _characterController.skinWidth * 2;
        _characterController.radius -= _characterController.skinWidth;

        // The collider is only active for remote connections to hit against.
        _capsuleCollider.enabled = !isLocalPlayer;

        // The character controller is only active for the controlling player.
        _characterController.enabled = isLocalPlayer;
    }

    public override void OnStartLocalPlayer()
    {
        // Keep a reference to the local player.
        GameManager.localPlayer = this;
        
        // Set the name of the player.
        SetNameCmd(GameManager.PlayerName);

        // Ensure all materials are rendering only shadows
        foreach (MeshRenderer mr in GetComponents<MeshRenderer>())
        {
            mr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
        {
            mr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    private void Update()
    {
        // On the local client, move.
        if (isLocalPlayer)
        {
            Movement();
            return;
        }

        // Ensure the character controller is not enabled on remote clients.
        _characterController.enabled = false;

        // On remote clients, aim the head where the player is looking.
        headPosition.localRotation = Quaternion.Euler(Mathf.Clamp(_rotationYRemote, -45, 45), 0, 0);
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
        _rotationYLocal = Mathf.Clamp(_rotationYLocal + -GameManager.Look.y * GameManager.Sensitivity * Time.deltaTime, -90, 90);

        // Sync vertical view across the network.
        if (NetworkClient.ready)
        {
            UpdateLookRotationCmd(_rotationYLocal);
        }
        
        // Update the local vertical rotation.
        cameraPosition.transform.localRotation = Quaternion.Euler(_rotationYLocal, 0, 0);
        
        // If on the ground.
        if (_characterController.isGrounded)
        {
            // Zero out any falling velocity.
            _velocityY = 0;
            
            // If requested to jump and are able to, jump.
            if (GameManager.Jump)
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
            // Cast down to see if we can stay on the ground to deal with slopes given the character is not set to be airborne.
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
        Vector3 targetVelocity = (GameManager.Move.y * tr.forward + GameManager.Move.x * tr.right) * GameManager.Speed;
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
}