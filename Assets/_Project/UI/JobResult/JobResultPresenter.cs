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
    /// <summary>Shows the customer's verdict when a job completes and sends FinishJobResult when dismissed.</summary>
    public sealed class JobResultPresenter : PresenterBase
    {
        private readonly IJobResultView _view;
        private readonly ICommandHandler<FinishJobResultCommand> _finish;

        public JobResultPresenter(IEventBus events, ICommandHandler<FinishJobResultCommand> finish, IJobResultView view)
        {
            _view = view;
            _finish = finish;

            Track(events.Subscribe<JobCompletedEvent>(_view.Show));
            Track(events.Subscribe<JobResultAcknowledgedEvent>(_ => _view.Hide()));
        }

        /// <summary>Called by the view's continue button.</summary>
        public void Continue() => _finish.Handle(new FinishJobResultCommand());
    }
}
