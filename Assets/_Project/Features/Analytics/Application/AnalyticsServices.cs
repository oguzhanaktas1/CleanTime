using System;
using System.Collections.Generic;
using CleanTime.Cleaning.Domain;
using CleanTime.Company.Domain;
using CleanTime.Employees.Domain;
using CleanTime.Equipment.Domain;
using CleanTime.Gifts.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Progression.Domain;
using CleanTime.Quests.Domain;
using CleanTime.Relationships.Domain;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Analytics.Application
{
    public sealed class AnalyticsEvent
    {
        public AnalyticsEvent(string name, IReadOnlyDictionary<string, object> properties = null)
        {
            Name = Guard.NotEmpty(name, nameof(name));
            Properties = properties ?? new Dictionary<string, object>();
        }

        public string Name { get; }

        public IReadOnlyDictionary<string, object> Properties { get; }
    }

    /// <summary>Fire-and-forget telemetry. Implementations must never block or throw into gameplay.</summary>
    public interface IAnalyticsService
    {
        void Track(AnalyticsEvent analyticsEvent);
    }

    /// <summary>Where buffered events finally go (HTTP, vendor SDK...). May fail; the buffer absorbs it.</summary>
    public interface IAnalyticsSink
    {
        bool TrySend(IReadOnlyList<AnalyticsEvent> batch);
    }

    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public void Track(AnalyticsEvent analyticsEvent)
        {
        }
    }

    /// <summary>Collects events and hands them to a sink in batches; drops the oldest when full.</summary>
    public sealed class BufferedAnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsSink _sink;
        private readonly int _capacity;
        private readonly Queue<AnalyticsEvent> _buffer = new Queue<AnalyticsEvent>();

        public BufferedAnalyticsService(IAnalyticsSink sink, int capacity = 200)
        {
            _sink = Guard.NotNull(sink, nameof(sink));
            _capacity = Math.Max(1, capacity);
        }

        public int Pending => _buffer.Count;

        public void Track(AnalyticsEvent analyticsEvent)
        {
            if (_buffer.Count >= _capacity)
                _buffer.Dequeue();

            _buffer.Enqueue(analyticsEvent);
        }

        /// <summary>Tries to deliver everything buffered. On failure the events stay queued.</summary>
        public bool Flush()
        {
            if (_buffer.Count == 0)
                return true;

            var batch = _buffer.ToArray();

            try
            {
                if (!_sink.TrySend(batch))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            _buffer.Clear();
            return true;
        }
    }

    /// <summary>Translates domain events into analytics events (architecture §87-88). Observes only.</summary>
    internal sealed class AnalyticsProjection : IDisposable
    {
        private readonly IAnalyticsService _analytics;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public AnalyticsProjection(IEventBus events, IAnalyticsService analytics)
        {
            _analytics = analytics;

            On<JobStartedEvent>(events, "job_started", e => ("job", e.DefinitionId.Value));
            On<JobCompletedEvent>(events, "job_completed", e => ("job", e.DefinitionId.Value), e => ("score", e.CustomerScore));
            On<JobFailedEvent>(events, "job_abandoned", e => ("job", e.DefinitionId.Value));
            On<CleaningTargetCompletedEvent>(events, "cleaning_target_completed", e => ("dirt", e.DirtTypeId.Value));
            On<EquipmentChangedEvent>(events, "equipment_selected", e => ("equipment", e.Current.Value));
            On<EquipmentPurchasedEvent>(events, "equipment_purchased", e => ("equipment", e.EquipmentId.Value));
            On<EquipmentUpgradedEvent>(events, "equipment_upgraded", e => ("equipment", e.EquipmentId.Value), e => ("level", e.UpgradeLevel));
            On<LocationUnlockedEvent>(events, "location_unlocked", e => ("location", e.LocationId.Value));
            On<CompanyCreatedEvent>(events, "company_created");
            On<EmployeeHiredEvent>(events, "employee_hired", e => ("employee", e.DefinitionId.Value));
            On<EmployeeJobCompletedEvent>(events, "employee_job_completed", e => ("quality", e.Quality));
            On<GiftGivenEvent>(events, "gift_given", e => ("gift", e.GiftId.Value));
            On<RelationshipLevelChangedEvent>(events, "relationship_level_changed", e => ("npc", e.NpcId.Value), e => ("level", e.Level));
            On<QuestCompletedEvent>(events, "quest_completed", e => ("quest", e.QuestId.Value));
            On<SaveCompletedEvent>(events, "save_created");
            On<SaveLoadedEvent>(events, "save_loaded");
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        private void On<T>(IEventBus events, string name, params Func<T, (string, object)>[] properties)
            where T : IDomainEvent
        {
            _subscriptions.Add(events.Subscribe<T>(evt =>
            {
                var bag = new Dictionary<string, object>();

                foreach (var property in properties)
                {
                    var (key, value) = property(evt);
                    bag[key] = value;
                }

                _analytics.Track(new AnalyticsEvent(name, bag));
            }));
        }
    }

    public sealed class AnalyticsModule : FeatureModule
    {
        public override string Name => "Analytics";

        public override void Register(IServiceRegistry services)
        {
            // Offline-safe default; a platform module may register a buffered, vendor-backed service later.
            services.RegisterSingleton<IAnalyticsService>(_ => new NullAnalyticsService());
            services.RegisterSingleton(r => new AnalyticsProjection(r.Resolve<IEventBus>(), r.Resolve<IAnalyticsService>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<AnalyticsProjection>();
        }
    }
}
