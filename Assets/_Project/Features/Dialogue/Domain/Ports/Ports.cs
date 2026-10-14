using CleanTime.Shared.Kernel;

namespace CleanTime.Dialogue.Domain
{
    public interface IDialogueDefinitionProvider : IDefinitionProvider<DialogueId, DialogueGraph>
    {
    }

    public interface IDialogueRepository
    {
        bool HasSeen(DialogueId id);

        void MarkSeen(DialogueId id);
    }
}
