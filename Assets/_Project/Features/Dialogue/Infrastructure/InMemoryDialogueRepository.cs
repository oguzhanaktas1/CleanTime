using System.Collections.Generic;
using CleanTime.Dialogue.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Dialogue.Infrastructure
{
    public sealed class InMemoryDialogueRepository : IDialogueRepository
    {
        private readonly HashSet<DialogueId> _seen = new HashSet<DialogueId>();

        public bool HasSeen(DialogueId id) => _seen.Contains(id);

        public void MarkSeen(DialogueId id) => _seen.Add(id);
    }

    public sealed class DialogueInfrastructureModule : FeatureModule
    {
        public override string Name => "Dialogue.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IDialogueRepository>(_ => new InMemoryDialogueRepository());
        }
    }
}
