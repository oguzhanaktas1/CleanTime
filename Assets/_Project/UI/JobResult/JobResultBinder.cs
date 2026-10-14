using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CleanTime.Jobs.Application;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;
using CleanTime.UI.Common;

namespace CleanTime.UI.JobResult
{
    public sealed class JobResultBinder : MonoBehaviour, ISceneService
    {
        [SerializeField] private JobResultView view;

        private JobResultPresenter _presenter;

        public void Construct(IServiceResolver services)
        {
            _presenter = new JobResultPresenter(
                services.Resolve<IEventBus>(),
                services.Resolve<ICommandHandler<FinishJobResultCommand>>(),
                view);
            view.ContinueClicked += _presenter.Continue;
        }

        private void OnDestroy()
        {
            if (view != null && _presenter != null)
                view.ContinueClicked -= _presenter.Continue;

            _presenter?.Dispose();
        }
    }
}
