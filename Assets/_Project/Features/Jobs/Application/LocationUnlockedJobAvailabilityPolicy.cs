using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;

namespace CleanTime.Jobs.Application
{
    /// <summary>A job is offered only while its location is unlocked.</summary>
    internal sealed class LocationUnlockedJobAvailabilityPolicy : IJobAvailabilityPolicy
    {
        private readonly ILocationProgressRepository _locations;

        public LocationUnlockedJobAvailabilityPolicy(ILocationProgressRepository locations)
        {
            _locations = locations;
        }

        public bool IsAvailable(JobDefinitionSnapshot definition)
        {
            var progress = _locations.Get(definition.LocationId);
            return progress.IsSuccess && progress.Value.Unlocked;
        }
    }
}
