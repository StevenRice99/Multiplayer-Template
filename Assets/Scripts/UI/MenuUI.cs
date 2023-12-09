using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// UI for the menu.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MenuUI : MonoBehaviour
    {
        /// <summary>
        /// Button to host a game.
        /// </summary>
        private Button _hostButton;
        
        /// <summary>
        /// Button to join a game.
        /// </summary>
        private Button _joinButton;
        
        /// <summary>
        /// Button to quit the game.
        /// </summary>
        private Button _quitButton;

        /// <summary>
        /// Field to input player name.
        /// </summary>
        private TextField _nameTextField;

        /// <summary>
        /// Field to input the address to join.
        /// </summary>
        private TextField _addressTextField;

        private void Start()
        {
            // Get the root.
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            
            // Store elements.
            _hostButton = root.Q<Button>("HostButton");
            _joinButton = root.Q<Button>("JoinButton");
            _quitButton = root.Q<Button>("QuitButton");
            _nameTextField = root.Q<TextField>("NameTextField");
            _addressTextField = root.Q<TextField>("AddressTextField");

            // Add button callbacks.
            _hostButton.clicked += NetworkManager.singleton.StartHost;
            _joinButton.clicked += NetworkManager.singleton.StartClient;
            _quitButton.clicked += Application.Quit;

            // Set initial field values.
            _nameTextField.value = GameManager.PlayerName;
            _addressTextField.value = NetworkManager.singleton.networkAddress;

            // Set field changing callbacks.
            _nameTextField.RegisterValueChangedCallback(NameChanged);
            _addressTextField.RegisterValueChangedCallback(AddressChanged);
        }

        private void OnDestroy()
        {
            // Cleanup any bindings.
            try
            {
                if (_hostButton != null)
                {
                    _hostButton.clicked -= NetworkManager.singleton.StartHost;
                }

                if (_joinButton != null)
                {
                    _joinButton.clicked -= NetworkManager.singleton.StartClient;
                }

                if (_hostButton != null)
                {
                    _quitButton.clicked -= Application.Quit;
                }

                _nameTextField?.UnregisterValueChangedCallback(NameChanged);
                _addressTextField?.UnregisterValueChangedCallback(AddressChanged);
            }
            catch { }
        }

        /// <summary>
        /// Update the name to the new name.
        /// </summary>
        /// <param name="evt">The event change data.</param>
        private void NameChanged(ChangeEvent<string> evt)
        {
            // If the string is empty, use the default name.
            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                GameManager.SetPlayerName(GameManager.DefaultPlayerName);
                return;
            }

            // Remove any invalid characters.
            string playerName = new(evt.newValue.Trim().ToCharArray().Where(x => !char.IsWhiteSpace(x)).ToArray());
            if (playerName != evt.newValue)
            {
                _nameTextField.value = playerName;
            }
            
            // Set the player name.
            GameManager.SetPlayerName(playerName);
            PlayerPrefs.SetString(nameof(GameManager.PlayerName), playerName);
        }

        /// <summary>
        /// Update the address to the new address.
        /// </summary>
        /// <param name="evt">The event change data.</param>
        private void AddressChanged(ChangeEvent<string> evt)
        {
            // If the string is empty, use the default address.
            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                NetworkManager.singleton.networkAddress = GameManager.DefaultAddress;
                return;
            }

            // Remove any invalid characters.
            string networkAddress = new(evt.newValue.Trim().ToCharArray().Where(x => !char.IsWhiteSpace(x)).ToArray());
            if (networkAddress != evt.newValue)
            {
                _addressTextField.value = networkAddress;
            }
            
            // Set the address.
            NetworkManager.singleton.networkAddress = networkAddress;
            PlayerPrefs.SetString(nameof(NetworkManager.singleton.networkAddress), networkAddress);
        }
    }
}