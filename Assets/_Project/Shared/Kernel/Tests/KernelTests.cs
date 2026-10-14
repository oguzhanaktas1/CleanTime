using System;
using System.Collections.Generic;
using NUnit.Framework;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Shared.Kernel.Tests
{
    public sealed class ResultTests
    {
        [Test]
        public void Success_HasNoError()
        {
            var result = Result.Success();

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.Error.HasValue);
            Assert.AreEqual(string.Empty, result.ErrorCode);
        }

        [Test]
        public void Failure_CarriesCodeAndMessage()
        {
            var result = Result.Failure("a.b", "boom");

            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual("a.b", result.ErrorCode);
            Assert.AreEqual("boom", result.Error.Value.Message);
        }

        [Test]
        public void TypedFailure_ConvertsToUntypedFailure()
        {
            Result<int> typed = Result<int>.Failure("x", "y");
            Result untyped = typed;

            Assert.IsTrue(untyped.IsFailure);
            Assert.AreEqual("x", untyped.ErrorCode);
        }

        [Test]
        public void TypedSuccess_ExposesValue()
        {
            var typed = Result<string>.Success("hello");

            Assert.IsTrue(typed.IsSuccess);
            Assert.AreEqual("hello", typed.Value);
        }
    }

    public sealed class MoneyTests
    {
        [Test]
        public void FromUnits_UsesIntegerCents()
        {
            Assert.AreEqual(1999, Money.FromUnits(19.99m).Cents);
        }

        [Test]
        public void Arithmetic_AddsAndSubtractsCents()
        {
            var total = new Money(250) + new Money(100) - new Money(50);

            Assert.AreEqual(300, total.Cents);
        }

        [Test]
        public void Scale_RoundsToNearestCent()
        {
            Assert.AreEqual(125, new Money(100).Scale(1.25m).Cents);
            Assert.AreEqual(33, new Money(100).Scale(0.333m).Cents);
        }

        [Test]
        public void Comparison_Works()
        {
            Assert.IsTrue(new Money(5) < new Money(6));
            Assert.IsTrue(new Money(-1).IsNegative);
            Assert.AreEqual(Money.Zero, new Money(0));
        }
    }

    public sealed class StrongIdTests
    {
        [Test]
        public void SameValue_IsEqual()
        {
            Assert.AreEqual(new LocationId("location.a"), new LocationId("location.a"));
            Assert.AreNotEqual(new LocationId("location.a"), new LocationId("location.b"));
        }

        [Test]
        public void DefaultId_IsEmpty()
        {
            Assert.IsTrue(default(NpcId).IsEmpty);
            Assert.IsFalse(new NpcId("npc.mother").IsEmpty);
        }

        [Test]
        public void IdsWorkAsDictionaryKeys()
        {
            var map = new Dictionary<EquipmentId, int> { { new EquipmentId("equipment.broom.basic"), 1 } };

            Assert.AreEqual(1, map[new EquipmentId("equipment.broom.basic")]);
        }

        [Test]
        public void Null_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new JobId(null));
        }
    }

    public sealed class GameDateTests
    {
        [Test]
        public void Ordering_ComparesDayThenPhase()
        {
            Assert.IsTrue(new GameDate(1, DayPhase.Night) < new GameDate(2, DayPhase.Morning));
            Assert.IsTrue(new GameDate(2, DayPhase.Evening) > new GameDate(2, DayPhase.Afternoon));
        }

        [Test]
        public void Day_MustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(0, DayPhase.Morning));
        }
    }

    public sealed class EventBusTests
    {
        private sealed class Ping : IDomainEvent
        {
            public int Value { get; set; }
        }

        [Test]
        public void Publish_DeliversToSubscribersInOrder()
        {
            var bus = new EventBus();
            var seen = new List<string>();
            bus.Subscribe<Ping>(_ => seen.Add("first"));
            bus.Subscribe<Ping>(_ => seen.Add("second"));

            bus.Publish(new Ping());

            CollectionAssert.AreEqual(new[] { "first", "second" }, seen);
        }

        [Test]
        public void Dispose_StopsDelivery()
        {
            var bus = new EventBus();
            var count = 0;
            var subscription = bus.Subscribe<Ping>(_ => count++);

            bus.Publish(new Ping());
            subscription.Dispose();
            bus.Publish(new Ping());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void ThrowingHandler_DoesNotBreakOthers()
        {
            var logger = new RecordingLogger();
            var bus = new EventBus(logger);
            var delivered = false;
            bus.Subscribe<Ping>(_ => throw new InvalidOperationException("bad"));
            bus.Subscribe<Ping>(_ => delivered = true);

            bus.Publish(new Ping());

            Assert.IsTrue(delivered);
            Assert.AreEqual(1, logger.Errors.Count);
        }

        [Test]
        public void UnsubscribeDuringDispatch_IsSafe()
        {
            var bus = new EventBus();
            var count = 0;
            IDisposable subscription = null;
            subscription = bus.Subscribe<Ping>(_ =>
            {
                count++;
                subscription.Dispose();
            });
            bus.Subscribe<Ping>(_ => count++);

            bus.Publish(new Ping());
            bus.Publish(new Ping());

            Assert.AreEqual(3, count);
        }
    }

    public sealed class ServiceContainerTests
    {
        private sealed class Leaf : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose() => Disposed = true;
        }

        private sealed class Branch
        {
            public Branch(Leaf leaf)
            {
                Leaf = leaf;
            }

            public Leaf Leaf { get; }
        }

        [Test]
        public void Singleton_IsCreatedOnce()
        {
            var container = new ServiceContainer();
            var created = 0;
            container.RegisterSingleton(_ => { created++; return new Leaf(); });

            Assert.AreSame(container.Resolve<Leaf>(), container.Resolve<Leaf>());
            Assert.AreEqual(1, created);
        }

        [Test]
        public void Transient_IsCreatedEveryTime()
        {
            var container = new ServiceContainer();
            container.RegisterTransient(_ => new Leaf());

            Assert.AreNotSame(container.Resolve<Leaf>(), container.Resolve<Leaf>());
        }

        [Test]
        public void Factories_CanResolveDependencies()
        {
            var container = new ServiceContainer();
            container.RegisterSingleton(_ => new Leaf());
            container.RegisterSingleton(r => new Branch(r.Resolve<Leaf>()));

            Assert.AreSame(container.Resolve<Leaf>(), container.Resolve<Branch>().Leaf);
        }

        [Test]
        public void Missing_ThrowsClearly()
        {
            var container = new ServiceContainer();

            Assert.Throws<ServiceNotRegisteredException>(() => container.Resolve<Leaf>());
            Assert.IsFalse(container.TryResolve<Leaf>(out _));
        }

        [Test]
        public void CircularDependency_IsDetected()
        {
            var container = new ServiceContainer();
            container.RegisterSingleton<Leaf>(r => { r.Resolve<Branch>(); return new Leaf(); });
            container.RegisterSingleton<Branch>(r => new Branch(r.Resolve<Leaf>()));

            Assert.Throws<InvalidOperationException>(() => container.Resolve<Leaf>());
        }

        [Test]
        public void ChildScope_FallsBackToParent_AndResolveAllCollectsBoth()
        {
            var parent = new ServiceContainer();
            parent.RegisterInstance("parent");
            var child = new ServiceContainer(parent);
            child.RegisterInstance("child");

            Assert.AreEqual("child", child.Resolve<string>());
            CollectionAssert.AreEqual(new[] { "parent", "child" }, child.ResolveAll<string>());
        }

        [Test]
        public void Dispose_DisposesCreatedSingletons_ButNotRegisteredInstances()
        {
            var container = new ServiceContainer();
            var external = new Leaf();
            container.RegisterInstance(external);
            container.RegisterSingleton<IDisposable>(_ => new Leaf());
            var created = (Leaf)container.Resolve<IDisposable>();

            container.Dispose();

            Assert.IsTrue(created.Disposed);
            Assert.IsFalse(external.Disposed);
        }
    }

    public sealed class StateMachineTests
    {
        private enum Light { Red, Green }

        [Test]
        public void AllowedTransition_Succeeds_AndRaisesChanged()
        {
            var machine = new StateMachine<Light>(Light.Red).Allow(Light.Red, Light.Green);
            Light? previous = null;
            machine.Changed += (from, _) => previous = from;

            var result = machine.TransitionTo(Light.Green);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(Light.Green, machine.Current);
            Assert.AreEqual(Light.Red, previous);
        }

        [Test]
        public void ForbiddenTransition_IsRejected()
        {
            var machine = new StateMachine<Light>(Light.Green).Allow(Light.Red, Light.Green);

            Assert.IsTrue(machine.TransitionTo(Light.Red).IsFailure);
            Assert.AreEqual(Light.Green, machine.Current);
        }
    }
}
