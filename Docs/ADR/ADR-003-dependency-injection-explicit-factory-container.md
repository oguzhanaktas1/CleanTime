# ADR-003: Dependency injection: explicit-factory container

- **Status:** Accepted (revisit if VContainer is adopted)
- **Architecture reference:** §10

## Context

The architecture allows VContainer, Extenject or a custom composition root. Reflection based containers struggle with IL2CPP stripping and with `internal` handlers.

## Decision

`ServiceContainer` (Shared Kernel) registers explicit factories: `services.RegisterSingleton<IFoo>(r => new Foo(r.Resolve<IBar>()))`. Modules implement `IFeatureModule` (Register, then Initialize). `FeatureRegistration` is the only place that lists all modules. Scene scopes create child containers and call `ISceneService.Construct`.

## Consequences

No reflection, no extra package, handlers can stay `internal`. Wiring is verbose but greppable. Swapping to VContainer only requires another `IServiceRegistry` implementation.
