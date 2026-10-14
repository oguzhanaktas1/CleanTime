using CleanTime.Shared.Kernel;

namespace CleanTime.Interaction.Domain
{
    /// <summary>Who is interacting and with what, without any Unity types.</summary>
    public sealed class InteractionContext
    {
        public InteractionContext(PlayerId playerId, EquipmentId selectedEquipment)
        {
            PlayerId = playerId;
            SelectedEquipment = selectedEquipment;
        }

        public PlayerId PlayerId { get; }

        public EquipmentId SelectedEquipment { get; }
    }

    /// <summary>
    /// Anything the player can interact with (door, NPC, vendor, bed...). Each interactable owns its
    /// behaviour; there is no central switch over interaction types.
    /// </summary>
    public interface IInteractable
    {
        string InteractionId { get; }

        /// <summary>Localization key for the prompt shown while the target is selected.</summary>
        string PromptKey { get; }

        bool CanInteract(InteractionContext context);

        Result Interact(InteractionContext context);
    }
}
