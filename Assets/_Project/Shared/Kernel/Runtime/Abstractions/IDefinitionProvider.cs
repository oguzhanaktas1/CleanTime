using System.Collections.Generic;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Read access to static (authored) content. Feature-specific aliases improve readability.</summary>
    public interface IDefinitionProvider<TId, TDefinition>
    {
        Result<TDefinition> Get(TId id);

        IReadOnlyCollection<TDefinition> GetAll();
    }
}
