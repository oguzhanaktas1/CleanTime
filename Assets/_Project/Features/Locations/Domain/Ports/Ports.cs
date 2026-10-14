using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Domain
{
    public interface ILocationDefinitionProvider : IDefinitionProvider<LocationId, LocationDefinitionSnapshot>
    {
    }

    public interface ILocationProgressRepository
    {
        Result<LocationProgress> Get(LocationId locationId);

        IReadOnlyList<LocationProgress> GetAll();

        Result Save(LocationProgress progress);
    }
}
