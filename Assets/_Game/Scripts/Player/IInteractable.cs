namespace Momentum.PlayerSystems
{
    /// <summary>Objects the player can use with the interact key (E).</summary>
    public interface IInteractable
    {
        /// <summary>Prompt shown on the HUD, e.g. "Pick up Shotgun". Return null/empty to hide.</summary>
        string GetInteractPrompt(PlayerController player);
        bool CanInteract(PlayerController player);
        void Interact(PlayerController player);
    }
}
