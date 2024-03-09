using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// UI for the lobby
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LobbyUI : MonoBehaviour
    {
        /// <summary>
        /// The list of players in the game.
        /// </summary>
        private Label _playerList;

        /// <summary>
        /// Display messages to the player.
        /// </summary>
        private Label _displayMessageLabel;

        /// <summary>
        /// The ready status of the player and the match.
        /// </summary>
        private Label _readyStatusLabel;

        private void Start()
        {
            // Get the root.
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            
            // Store elements.
            _playerList = root.Q<Label>("PlayerList");
            _displayMessageLabel = root.Q<Label>("DisplayMessage");
            _readyStatusLabel = root.Q<Label>("ReadyStatus");
        }

        private void Update()
        {
            // Keep lists updated.
            _playerList.text = BuildPlayerList();

            // Display the correct message.
            _displayMessageLabel.text = GameManager.DisplayMessage;
            
            // Determine if ready to start.
            _readyStatusLabel.text = ReadyToStart();
        }

        /// <summary>
        /// Build a string list of players.
        /// </summary>
        /// <returns>A string of all players.</returns>
        private static string BuildPlayerList()
        {
            return string.Join("\n", GameManager.players.OrderBy(x => x.PlayerName).Select(x => x.PlayerName));
        }

        /// <summary>
        /// Determine the message to display based on if ready to start the match.
        /// </summary>
        /// <returns>The string to display about the state to start the match.</returns>
        private static string ReadyToStart()
        {
            // Get if the local player is ready.
            string msg = GameManager.localPlayer != null && GameManager.localPlayer.Ready ? "Ready\n" : "Not Ready\n";

            // If all players are ready display that the match is starting.
            return GameManager.ReadyPlayers == GameManager.players.Count ? $"{msg}Starting match..." :
                // Display how many players are ready otherwise.
                $"{msg}{GameManager.ReadyPlayers} of {GameManager.players.Count} ready";
        }
    }
}