namespace Boruhan.Story
{
    /// <summary>Anything the player can face and press E on (Interactable, GameEvent…).</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact();
    }
}
