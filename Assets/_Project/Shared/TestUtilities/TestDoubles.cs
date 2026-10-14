using System;
using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Shared.TestUtilities
{
    public sealed class FakeGameClock : IGameClock
    {
        public GameDate Now { get; set; } = GameDate.Start;
    }

    public sealed class FakeUtcClock : IUtcClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public void Advance(TimeSpan span) => UtcNow += span;
    }

    public sealed class SequentialIdGenerator : IIdGenerator
    {
        private int _next;

        public string NewId() => $"id-{++_next:D4}";
    }

    public sealed class RecordingLogger : ILogger
    {
        public List<string> Errors { get; } = new List<string>();

        public List<string> Messages { get; } = new List<string>();

        public void Trace(string message) => Messages.Add(message);

        public void Debug(string message) => Messages.Add(message);

        public void Info(string message) => Messages.Add(message);

        public void Warning(string message) => Messages.Add(message);

        public void Error(string message, Exception ex = null) => Errors.Add(message);

        public void Fatal(string message, Exception ex = null) => Errors.Add(message);
    }

    /// <summary>Collects every event of type T published on the bus.</summary>
    public sealed class EventRecorder<T> : IDisposable
        where T : IDomainEvent
    {
        private readonly IDisposable _subscription;

        public EventRecorder(IEventBus bus)
        {
            _subscription = bus.Subscribe<T>(Events.Add);
        }

        public List<T> Events { get; } = new List<T>();

        public int Count => Events.Count;

        public T Last => Events[Events.Count - 1];

        public void Dispose() => _subscription.Dispose();
    }

    public static class EventBusTestExtensions
    {
        public static EventRecorder<T> Record<T>(this IEventBus bus)
            where T : IDomainEvent => new EventRecorder<T>(bus);
    }
}
