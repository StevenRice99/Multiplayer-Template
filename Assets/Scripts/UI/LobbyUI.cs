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
        /// The players that do not yet have a team.
        /// </summary>
        private Label _noneListLabel;

        /// <summary>
        /// The players on the red team.
        /// </summary>
        private Label _redPlayersListLabel;

        /// <summary>
        /// The players on the blue team.
        /// </summary>
        private Label _bluePlayersListLabel;

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
            _noneListLabel = root.Q<Label>("NoneList");
            _redPlayersListLabel = root.Q<Label>("RedPlayersList");
            _bluePlayersListLabel = root.Q<Label>("BluePlayersList");
            _displayMessageLabel = root.Q<Label>("DisplayMessage");
            _readyStatusLabel = root.Q<Label>("ReadyStatus");
        }

        private void Update()
        {
            // Keep lists updated.
            _noneListLabel.text = BuildPlayerList(Team.None);
            _redPlayersListLabel.text = BuildPlayerList(Team.Red);
            _bluePlayersListLabel.text = BuildPlayerList(Team.Blue);

            // Display the correct message.
            _displayMessageLabel.text = GameManager.DisplayMessage;
            
            // Determine if ready to start.
            _readyStatusLabel.text = ReadyToStart();
        }

        /// <summary>
        /// Build a string list of players on a team.
        /// </summary>
        /// <param name="team">The team to get the players on.</param>
        /// <returns>A string of all team members on newlines.</returns>
        private static string BuildPlayerList(Team team)
        {
            return string.Join("\n", GameManager.players.Where(x => x.Team == team).Select(x => x.PlayerName));
        }

        /// <summary>
        /// Determine the message to display based on if ready to start the match.
        /// </summary>
        /// <returns>The string to display about the state to start the match.</returns>
        private static string ReadyToStart()
        {
            // Get if the local player is ready.
            string msg = GameManager.localPlayer != null && GameManager.localPlayer.Ready ? "Ready\n" : "Not Ready\n";
            
            // There are no players on either team.
            if (GameManager.RedPlayers < 1 && GameManager.BluePlayers < 1)
            {
                return $"{msg}No players on Red or Blue";
            }

            // There are no players on the red team.
            if (GameManager.RedPlayers < 1)
            {
                return $"{msg}No players on Red";
            }

            // There are no players on the blue team.
            if (GameManager.BluePlayers < 1)
            {
                return $"{msg}No players on Blue";
            }
            
            // If there are players not yet on a team, they need to choose one, although this should be automatic.
            if (GameManager.RedPlayers + GameManager.BluePlayers != GameManager.players.Count)
            {
                return $"{msg}Waiting for players to pick teams";
            }

            // If all players are ready display that the match is starting.
            if (GameManager.ReadyPlayers == GameManager.players.Count)
            {
                return $"{msg}Starting match...";
            }

            // Display how many players are ready otherwise.
            return $"{msg}{GameManager.ReadyPlayers} of {GameManager.players.Count} ready";
        }
    }
}