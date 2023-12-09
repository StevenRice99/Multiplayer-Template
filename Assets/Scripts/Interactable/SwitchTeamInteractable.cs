using UnityEngine;

namespace Interactable
{
    /// <summary>
    /// Switch teams.
    /// </summary>
    public class SwitchTeamInteractable : InteractableBase
    {
        /// <summary>
        /// What team to switch to when interacted with.
        /// </summary>
        [SerializeField]
        [Tooltip("What team to switch to when interacted with.")]
        private Team team = Team.Red;
        
        /// <summary>
        /// Set the team of the local player.
        /// </summary>
        public override void Interact()
        {
            GameManager.localPlayer.SetTeamCmd(team);
        }
    }
}