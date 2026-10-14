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
    public interface IJobResultView
    {
        void Show(JobCompletedEvent result);

        void Hide();
    }
}
