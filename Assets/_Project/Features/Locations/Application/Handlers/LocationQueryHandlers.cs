using System.Collections.Generic;
using System.Linq;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Locations.Application
{
    internal sealed class GetUnlockedLocationsHandler
        : IQueryHandler<GetUnlockedLocationsQuery, IReadOnlyList<LocationDefinitionSnapshot>>
    {
        private readonly ILocationDefinitionProvider _definitions;
        private readonly ILocationProgressRepository _progress;

        public GetUnlockedLocationsHandler(ILocationDefinitionProvider definitions, ILocationProgressRepository progress)
        {
            _definitions = definitions;
            _progress = progress;
        }

        public IReadOnlyList<LocationDefinitionSnapshot> Handle(GetUnlockedLocationsQuery query)
        {
            var unlocked = new HashSet<LocationId>(_progress.GetAll().Where(p => p.Unlocked).Select(p => p.LocationId));

            return _definitions.GetAll()
                .Where(d => unlocked.Contains(d.Id))
                .OrderBy(d => d.Tier)
                .ToList();
        }
    }

    internal sealed class GetLocationProgressHandler : IQueryHandler<GetLocationProgressQuery, Result<LocationProgress>>
    {
        private readonly ILocationProgressRepository _progress;

        public GetLocationProgressHandler(ILocationProgressRepository progress)
        {
            _progress = progress;
        }

        public Result<LocationProgress> Handle(GetLocationProgressQuery query) => _progress.Get(query.LocationId);
    }
}
