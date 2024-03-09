namespace Interact
{
    /// <summary>
    /// Toggle ready state.
    /// </summary>
    public class ReadyInteract : InteractBase
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