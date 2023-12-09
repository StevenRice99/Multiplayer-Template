namespace Interactable
{
    /// <summary>
    /// Toggle ready state.
    /// </summary>
    public class ReadyInteractable : InteractableBase
    {
        /// <summary>
        /// Toggle the ready state of the local player.
        /// </summary>
        public override void Interact()
        {
            GameManager.localPlayer.SetReadyCmd(!GameManager.localPlayer.Ready);
        }
    }
}