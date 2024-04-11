using System;
using System.Collections.Generic;
using System.Linq;
using Interact;
using Mirror;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

/// <summary>
/// Core component to control the game.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(Transport))]
public class GameManager : NetworkManager
{
    /// <summary>
    /// The default address to connect to.
    /// </summary>
    public const string DefaultAddress = "localhost";
    
    /// <summary>
    /// The default name for the player.
    /// </summary>
    public const string DefaultPlayerName = "Anonymous";

    /// <summary>
    /// The default sensitivity for the mouse.
    /// </summary>
    private const float DefaultSensitivity = 10;

    /// <summary>
    /// The default resolution to play in.
    /// </summary>
    private const int DefaultResolution = 512;

    /// <summary>
    /// The default audio level.
    /// </summary>
    private const int DefaultAudio = 0;
    
    /// <summary>
    /// The key to store the fullscreen preference.
    /// </summary>
    private const string FullscreenKey = "Fullscreen";
    
    /// <summary>
    /// The key to store the width preference.
    /// </summary>
    private const string WidthKey = "Width";
    
    /// <summary>
    /// The key to store the height preference.
    /// </summary>
    private const string HeightKey = "Height";

    /// <summary>
    /// The local player.
    /// </summary>
    public static PlayerController localPlayer;

    /// <summary>
    /// All players in the game.
    /// </summary>
    public static readonly HashSet<PlayerController> players = new();

    /// <summary>
    /// All objects you can interact with in the scene.
    /// </summary>
    public static readonly HashSet<InteractBase> interactObjects = new();

    /// <summary>
    /// The messages to display on the UI.
    /// </summary>
    public static string DisplayMessage { get; private set; } = string.Empty;

    /// <summary>
    /// The local player's name.
    /// </summary>
    public static string PlayerName { get; private set; } = DefaultPlayerName;
    
    /// <summary>
    /// The number of players that are ready.
    /// </summary>
    public static int ReadyPlayers { get; private set; }

    /// <summary>
    /// The sound level.
    /// </summary>
    public static float Sound { get; private set; }

    /// <summary>
    /// How fast the players move.
    /// </summary>
    public static float Speed => Instance.speed;

    /// <summary>
    /// How much force the player jumps with.
    /// </summary>
    public static float JumpForce => Instance.jumpForce;

    /// <summary>
    /// How far to cast for ground detection.
    /// </summary>
    public static float GroundedDistance => Instance.groundedDistance;

    /// <summary>
    /// The movement for the local player.
    /// </summary>
    public static Vector2 Move => Instance._optionsOpen ? Vector2.zero : Instance._move;
    
    /// <summary>
    /// The looking for the local player.
    /// </summary>
    public static Vector2 Look => Instance._optionsOpen ? Vector2.zero : Instance._look;
    
    /// <summary>
    /// Jump controls for the local player.
    /// </summary>
    public static bool Jump => !Instance._optionsOpen && Instance._jump;

    /// <summary>
    /// Look sensitivity.
    /// </summary>
    public static float Sensitivity => Instance._optionsOpen ? 0 : Instance._sensitivity;

    /// <summary>
    /// Singleton instance of this game manager.
    /// </summary>
    private static GameManager Instance => singleton as GameManager;

    /// <summary>
    /// If currently in the lobby.
    /// </summary>
    public static bool IsLobby => networkSceneName == singleton.onlineScene || SceneManager.GetActiveScene().name == "Lobby";

    /// <summary>
    /// The transform of the local player's camera.
    /// </summary>
    private static Transform CameraPosition => localPlayer != null ? localPlayer.CameraPosition : null;

    /// <summary>
    /// Names of all the maps so map rotation knows what to load.
    /// </summary>
    [Header("Level")]
    [SerializeField]
    [Tooltip("The level to play on.")]
    private string level;

    /// <summary>
    /// How fast the players move.
    /// </summary>
    [Header("Player")]
    [SerializeField]
    [Min(float.Epsilon)]
    [Tooltip("How fast the players move.")]
    private float speed = 15;

    /// <summary>
    /// How much force the player jumps with.
    /// </summary>
    [SerializeField]
    [Min(float.Epsilon)]
    [Tooltip("How much force the player jumps with.")]
    private float jumpForce = 6;

    /// <summary>
    /// How far to cast for ground detection.
    /// </summary>
    [SerializeField]
    [Min(float.Epsilon)]
    [Tooltip("How far to cast for ground detection.")]
    private float groundedDistance = 1.5f;

    /// <summary>
    /// Cache of the main camera.
    /// </summary>
    private Camera _cam;

    /// <summary>
    /// Movement data.
    /// </summary>
    private Vector2 _move;

    /// <summary>
    /// Look data.
    /// </summary>
    private Vector2 _look;

    /// <summary>
    /// Jump data.
    /// </summary>
    private bool _jump;

    /// <summary>
    /// Sensitivity data.
    /// </summary>
    private float _sensitivity;

    /// <summary>
    /// Interact data.
    /// </summary>
    private bool _interact;

    /// <summary>
    /// The root of the options visual.
    /// </summary>
    private VisualElement _optionsRoot;

    /// <summary>
    /// Sensitivity selector.
    /// </summary>
    private FloatField _sensitivityFloatField;
    
    /// <summary>
    /// Close options button.
    /// </summary>
    private Button _closeButton;
    
    /// <summary>
    /// Return to lobby button.
    /// </summary>
    private Button _lobbyButton;
    
    /// <summary>
    /// Leave the game button.
    /// </summary>
    private Button _leaveButton;

    /// <summary>
    /// Sound slider.
    /// </summary>
    private Slider _soundSlider;

    /// <summary>
    /// If the options menu is open or not.
    /// </summary>
    private bool _optionsOpen;

    /// <summary>
    /// List of other documents in the scene.
    /// </summary>
    private List<UIDocument> _otherUIDocuments;

    /// <summary>
    /// Check if this is the server.
    /// </summary>
    private bool IsServer => isNetworkActive && mode is NetworkManagerMode.Host or NetworkManagerMode.ServerOnly;

    /// <summary>
    /// Check if this is a client.
    /// </summary>
    private bool IsClient => isNetworkActive && mode is NetworkManagerMode.Host or NetworkManagerMode.ClientOnly;

    /// <summary>
    /// Get other documents in the scene.
    /// </summary>
    private IEnumerable<UIDocument> OtherUIDocuments
    {
        get
        {
            // If there are no null documents, return it.
            if (_otherUIDocuments is {Count: > 0} && _otherUIDocuments.All(x => x != null))
            {
                return _otherUIDocuments;
            }

            // Otherwise find and return other documents.
            UIDocument thisUIDocument = GetComponent<UIDocument>();
            _otherUIDocuments = FindObjectsOfType<UIDocument>().Where(u => u != thisUIDocument).ToList();
            return _otherUIDocuments;
        }
    }

    public override void Awake()
    {
        // Ensure there is only one manager.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        base.Awake();

        // Load values.
        SetResolution(PlayerPrefs.GetInt(FullscreenKey) != 0, true);
        networkAddress = PlayerPrefs.GetString(nameof(networkAddress), DefaultAddress).Trim();
        PlayerName = PlayerPrefs.GetString(nameof(PlayerName), DefaultPlayerName).Trim();
        _sensitivity = PlayerPrefs.GetFloat(nameof(Sensitivity), DefaultSensitivity);
        Sound = PlayerPrefs.GetFloat(nameof(Sound), DefaultAudio);
    }

    public override void Start()
    {
        base.Start();
        
        // Get the root.
        _optionsRoot = GetComponent<UIDocument>().rootVisualElement;
        
        // Store elements.
        _closeButton = _optionsRoot.Q<Button>("CloseButton");
        _lobbyButton = _optionsRoot.Q<Button>("LobbyButton");
        _leaveButton = _optionsRoot.Q<Button>("LeaveButton");
        _soundSlider = _optionsRoot.Q<Slider>("SoundSlider");
        _sensitivityFloatField = _optionsRoot.Q<FloatField>("SensitivityFloatField");
        
        // Add button callbacks.
        _closeButton.clicked += HideOptions;
        _lobbyButton.clicked += ReturnToLobby;
        _leaveButton.clicked += LeaveMatch;

        // Set field changing callbacks.
        _soundSlider.RegisterValueChangedCallback(SoundChanged);
        _sensitivityFloatField.RegisterValueChangedCallback(SensitivityChanged);
        
        // Set initial field values.
        _soundSlider.value = Sound;
        _sensitivityFloatField.value = _sensitivity;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        // Cleanup buttons.
        if (_closeButton != null)
        {
            _closeButton.clicked -= HideOptions;
        }

        if (_lobbyButton != null)
        {
            _lobbyButton.clicked -= ReturnToLobby;
        }

        if (_leaveButton != null)
        {
            _leaveButton.clicked -= LeaveMatch;
        }

        _sensitivityFloatField?.UnregisterValueChangedCallback(SensitivityChanged);
        _soundSlider?.UnregisterValueChangedCallback(SoundChanged);

        // Save the screen resolution.
        SaveResolution();
    }
    
    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        
        // Save the screen resolution.
        SaveResolution();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();

        // Handle the cursor.
        HandleCursor();

        // Handle displaying the options.
        HandleOptions();

        // If in the lobby, display ready to start messages.
        if (IsLobby)
        {
            ReadyToStart();
        }
        
        if (!IsClient)
        {
            return;
        }
        
        // Handle objects you can interact with and the camera on clients (not standalone servers).
        HandleInteractions();
        HandleCamera();
    }

    /// <summary>
    /// Handle the logic of all objects you can interact with.
    /// </summary>
    private void HandleInteractions()
    {
        // Do not handle if the options menu is open.
        if (_optionsOpen || CameraPosition == null)
        {
            return;
        }

        // Get the nearest object you can interact with in range.
        InteractBase interactBase = interactObjects.Where(i => Vector3.Distance(CameraPosition.position, i.transform.position) <= i.ActivationDistance).OrderBy(i => Vector3.Distance(CameraPosition.position, i.transform.position)).FirstOrDefault();

        // If none, have an empty message.
        if (interactBase == null)
        {
            DisplayMessage = string.Empty;
            return;
        }

        // Otherwise set the message.
        DisplayMessage = interactBase.DisplayMessage;
        
        if (!_interact)
        {
            return;
        }

        // When the user interacts, call the interact method.
        interactBase.Interact();
        _interact = false;
    }

    /// <summary>
    /// Handle the camera.
    /// </summary>
    private void HandleCamera()
    {
        // If the camera is null, try and find it.
        if (_cam == null)
        {
            _cam = Camera.main;
            
            // If there is no camera tagged as the main camera, try and find any camera in the scene.
            if (_cam == null)
            {
                _cam = FindObjectOfType<Camera>();
            
                // If there is still no camera, return.
                if (_cam == null)
                {
                    return;
                }
            }
        }
        
        Transform camTransform = _cam.transform;
        
        // Set to the local player if there is one.
        if (CameraPosition != null)
        {
            camTransform.position = CameraPosition.position;
            camTransform.rotation = CameraPosition.rotation;
        }

        transform.position = camTransform.position;
    }

    /// <summary>
    /// Set the name of the player.
    /// </summary>
    /// <param name="playerName">The name for the player.</param>
    public static void SetPlayerName(string playerName)
    {
        PlayerName = playerName.Trim();
    }

    /// <summary>
    /// Check if ready to start the game.
    /// </summary>
    private void ReadyToStart()
    {
        // Get the number of players that are ready.
        ReadyPlayers = players.Count(player => player.Ready);

        // If all players are ready and there is at least one player on each team, start the game.
        if (IsServer && !NetworkServer.isLoadingScene && players.Count > 0 && ReadyPlayers == players.Count)
        {
            singleton.ServerChangeScene(Instance.level);
        }
    }

    /// <summary>
    /// Return to the lobby.
    /// </summary>
    private void ReturnToLobby()
    {
        // Only called on the server.
        if (!IsServer)
        {
            return;
        }
        
        // Change back to the lobby.
        ServerChangeScene(onlineScene);
        _optionsOpen = false;
    }
    
    /// <summary>
    /// Callback for when the sensitivity is changed.
    /// </summary>
    /// <param name="evt">The change event for the sensitivity.</param>
    private void SensitivityChanged(ChangeEvent<float> evt)
    {
        // Ensure the sensitivity is valid.
        _sensitivity = Mathf.Max(Math.Abs(evt.newValue), 0.01f);
        
        // Save the sensitivity.
        PlayerPrefs.SetFloat(nameof(Sensitivity), _sensitivity);
    }

    /// <summary>
    /// Callback for when the sound is changed.
    /// </summary>
    /// <param name="evt">The change event for the sensitivity.</param>
    private static void SoundChanged(ChangeEvent<float> evt)
    {
        // Set the sound.
        Sound = math.clamp(evt.newValue, 0, 1);
        AudioListener.volume = Sound;
        
        // Save the sound.
        PlayerPrefs.SetFloat(nameof(Sound), Sound);
    }

    /// <summary>
    /// Hide the options.
    /// </summary>
    private void HideOptions()
    {
        _optionsOpen = false;
    }

    /// <summary>
    /// Handle if the cursor is shown or not.
    /// </summary>
    private void HandleCursor()
    {
        // Visible if in the menu or the options are open.
        bool visible = !IsServer && !IsClient || _optionsOpen;
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Handle rendering the options.
    /// </summary>
    private void HandleOptions()
    {
        // Ensure other UI are the opposite display of the options (one is shown, the other is not).
        foreach (VisualElement root in OtherUIDocuments.Select(otherUIDocument => otherUIDocument.rootVisualElement))
        {
            root.visible = !_optionsOpen;
            root.style.display = !_optionsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            root.SetEnabled(!_optionsOpen);
        }

        _optionsRoot.visible = _optionsOpen;
        _optionsRoot.SetEnabled(_optionsOpen);
        _optionsRoot.style.display = _optionsOpen ? DisplayStyle.Flex : DisplayStyle.None;

        // Nothing else to do if the options are not open.
        if (!_optionsOpen)
        {
            return;
        }

        // Determine if the return to lobby button is visible being only for the hosts in the match.
        bool visible = !IsLobby && IsServer;
        _lobbyButton.visible = visible;
        _lobbyButton.SetEnabled(visible);
        _lobbyButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // Check if the leave button is visible being only for when connected to a match.
        visible = IsServer || IsClient;
        _leaveButton.visible = visible;
        _leaveButton.SetEnabled(visible);
        _leaveButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Leave a match.
    /// </summary>
    private void LeaveMatch()
    {
        // Act depending on mode.
        switch (mode)
        {
            // In a standalone server, shutdown.
            case NetworkManagerMode.ServerOnly:
                StopServer();
                Application.Quit();
                break;
            // For a client, stop the client.
            case NetworkManagerMode.ClientOnly:
                StopClient();
                break;
            // For a host, stop the host.
            case NetworkManagerMode.Host:
                StopHost();
                break;
            // Otherwise, nothing to be done.
            case NetworkManagerMode.Offline:
            default:
                break;
        }
        
        _optionsOpen = false;
    }

    /// <summary>
    /// Set the resolution.
    /// </summary>
    /// <param name="fullscreen">If in fullscreen.</param>
    /// <param name="initial">If this is the initial configuration</param>
    private static void SetResolution(bool fullscreen, bool initial = false)
    {
        // Get the saved width and height.
        int width = PlayerPrefs.GetInt(WidthKey);
        int height = PlayerPrefs.GetInt(HeightKey);
        
        // Set the fullscreen value.
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);

        // If in fullscreen set it.
        if (fullscreen)
        {
            // If not the initial load, update the desired non-fullscreen values.
            if (!initial)
            {
                PlayerPrefs.SetInt(WidthKey, Screen.width);
                PlayerPrefs.SetInt(HeightKey, Screen.height);
            }
            
            // Set to fullscreen.
            Resolution resolution = Screen.resolutions.OrderByDescending(r => r.width * r.height).FirstOrDefault();
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.ExclusiveFullScreen);
            return;
        }

        // Ensure valid sizing.
        if (width <= 0)
        {
            width = DefaultResolution;
        }

        if (height <= 0)
        {
            height = DefaultResolution;
        }
        
        // Set the window to the size.
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
    }

    /// <summary>
    /// Save the resolution.
    /// </summary>
    private static void SaveResolution()
    {
        // Save the fullscreen value.
        PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : 1);

        // Only save other details if not in fullscreen.
        if (Screen.fullScreenMode != FullScreenMode.Windowed)
        {
            return;
        }
        
        PlayerPrefs.SetInt(WidthKey, Screen.width);
        PlayerPrefs.SetInt(HeightKey, Screen.height);
    }

    /// <summary>
    /// Movement input callback.
    /// </summary>
    /// <param name="value">Movement input.</param>
    private void OnMove(InputValue value)
    {
        _move = value.Get<Vector2>();
    }

    /// <summary>
    /// Look input callback.
    /// </summary>
    /// <param name="value">Look input.</param>
    private void OnLook(InputValue value)
    {
        _look = value.Get<Vector2>();
    }
    
    /// <summary>
    /// Jump input callback.
    /// </summary>
    /// <param name="value">Jump input.</param>
    private void OnJump(InputValue value)
    {
        _jump = value.isPressed;
    }
    
    /// <summary>
    /// Interact input callback.
    /// </summary>
    /// <param name="value">Interact input.</param>
    private void OnInteract(InputValue value)
    {
        _interact = value.isPressed;
    }

    /// <summary>
    /// Escape input callback.
    /// </summary>
    /// <param name="value">Escape input.</param>
    private void OnEscape(InputValue value)
    {
        _optionsOpen = !_optionsOpen;
    }

    /// <summary>
    /// Fullscreen input callback.
    /// </summary>
    /// <param name="value">Fullscreen input.</param>
    private void OnFullscreen(InputValue value)
    {
        SetResolution(Screen.fullScreenMode == FullScreenMode.Windowed);
    }
}