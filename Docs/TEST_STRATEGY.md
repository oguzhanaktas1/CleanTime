# Test strategy

```text
            PlayMode smoke (Bootstrap/PlayModeTests)      boots the real scenes
          Integration + architecture (Bootstrap/Tests)    real modules via FeatureRegistration, fakes for time/ids/scenes
        Feature unit tests (Features/<X>/Tests)           handlers, domain rules, repositories, participants
      Kernel tests (Shared/Kernel/Tests)                  Result, Money, IDs, event bus, DI container, state machine
```

Run everything in batch mode (editor must be closed) or from the Test Runner window:

```bash
Tools/run-tests.sh editmode
Tools/run-tests.sh playmode
```

- Test names describe behaviour (`Location_ShouldUnlock_AfterSixCompletions`) and mirror the cases in architecture §100-106.
- `Shared/TestUtilities` has `FakeGameClock`, `FakeUtcClock`, `SequentialIdGenerator`, `RecordingLogger` and `bus.Record<TEvent>()`.
- Internals of an `Application`/`Domain` assembly are visible to `CleanTime.<Feature>.Tests` (generated `AssemblyInfo.cs`).
- `GameHarness` (Bootstrap/Tests) composes the real game from `FeatureRegistration.CreateGameplayModules` and builds content with
  `TestAssets`. Use it for cross-module scenarios (job -> reward -> progression -> quests -> save).
- Clock and ids are fakes, so runs are deterministic. Randomness uses seeded `IRandomSource`.
- `ArchitectureTests` fail the build when Domain/Application reference Unity, when a dependency points outwards, or when a
  MonoBehaviour / ScriptableObject is not in a file named after its class.
- PlayMode tests fail on any logged error / exception (for example a missing script or a DI failure at boot).
