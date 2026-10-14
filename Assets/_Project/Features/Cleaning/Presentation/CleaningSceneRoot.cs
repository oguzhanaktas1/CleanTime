using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CleanTime.Audio.Application;
using CleanTime.Cleaning.Application;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>
    /// Scene-level entry point of the cleaning feature: gathers the authored targets, exposes them as
    /// start-up seeds and wires presenters / feedback. The session itself is started by whoever knows
    /// the active job (see the property scene binder in the Bootstrap assembly).
    /// </summary>
    public sealed class CleaningSceneRoot : MonoBehaviour, ISceneService
    {
        [Tooltip("Business id of the location this scene represents (e.g. location.apartment.small.001).")]
        [SerializeField] private string locationId = "";
        [SerializeField] private CleaningFeedbackView feedbackView;
        [SerializeField] private WrongToolFeedbackView wrongToolView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly Dictionary<string, CleaningTargetView> _views = new Dictionary<string, CleaningTargetView>();
        private CleaningTargetAuthoring[] _authoring = new CleaningTargetAuthoring[0];

        public LocationId LocationId => new LocationId(locationId ?? string.Empty);

        public CleaningProgressHudPresenter ProgressPresenter { get; private set; }

        /// <summary>Optional: assign a view to receive progress updates.</summary>
        public void BindProgressView(IEventBus events, ICleaningProgressView view)
        {
            ProgressPresenter?.Dispose();
            ProgressPresenter = new CleaningProgressHudPresenter(events, view);
            _disposables.Add(ProgressPresenter);
        }

        public void Construct(IServiceResolver services)
        {
            _authoring = GetComponentsInChildren<CleaningTargetAuthoring>(true);

            foreach (var authoring in _authoring)
            {
                authoring.View.Initialize(authoring.StableInstanceId);
                _views[authoring.StableInstanceId] = authoring.View;
            }

            var events = services.Resolve<IEventBus>();
            _disposables.Add(new CleaningTargetPresenter(events, _views));
            _disposables.Add(new CleaningFeedbackPresenter(events, services.Resolve<IAudioService>(), feedbackView, wrongToolView));
        }

        /// <summary>Validated start-up data for <see cref="StartCleaningSessionCommand"/>.</summary>
        public IReadOnlyList<CleaningTargetSeed> BuildSeeds(out string problems)
        {
            var seeds = new List<CleaningTargetSeed>();
            var errors = new List<string>();

            foreach (var authoring in _authoring.Where(a => a.isActiveAndEnabled))
            {
                if (authoring.IsValid(out var problem))
                    seeds.Add(authoring.ToSeed());
                else
                    errors.Add($"{authoring.name}: {problem}");
            }

            problems = string.Join("; ", errors);
            return seeds;
        }

        private void OnDestroy()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();

            _disposables.Clear();
        }
    }
}
