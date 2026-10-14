using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Application
{
    public sealed record ApplyRelationshipChangeCommand(NpcId NpcId, int Delta) : ICommand;

    public sealed record GetRelationshipQuery(NpcId NpcId) : IQuery<Result<Relationship>>;
}
