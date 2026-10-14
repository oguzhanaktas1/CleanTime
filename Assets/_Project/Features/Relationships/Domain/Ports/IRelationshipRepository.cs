using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Domain
{
    public interface IRelationshipRepository
    {
        /// <summary>Returns the relationship, creating a level-0 one the first time an NPC is seen.</summary>
        Result<Relationship> Get(NpcId npcId);

        IReadOnlyList<Relationship> GetAll();

        Result Save(Relationship relationship);
    }
}
