using System.Collections.Generic;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Application
{
    public sealed record GetUnlockedLocationsQuery : IQuery<IReadOnlyList<LocationDefinitionSnapshot>>;

    public sealed record GetLocationProgressQuery(LocationId LocationId) : IQuery<Result<LocationProgress>>;
}
