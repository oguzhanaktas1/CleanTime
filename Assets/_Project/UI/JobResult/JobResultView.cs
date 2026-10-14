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
    public sealed class JobResultView : MonoBehaviour, IJobResultView
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private TMP_Text ratingLabel;
        [SerializeField] private TMP_Text detailLabel;
        [SerializeField] private Button continueButton;

        public event Action ContinueClicked;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());

            Hide();
        }

        public void Show(JobCompletedEvent result)
        {
            if (root != null) root.SetActive(true);
            if (rewardLabel != null) rewardLabel.text = result.Reward.ToString();
            if (ratingLabel != null) ratingLabel.text = new string('*', result.Evaluation.Stars);
            if (detailLabel != null)
                detailLabel.text = $"Cleaning {result.Evaluation.Cleaning:P0}  Efficiency {result.Evaluation.Efficiency:P0}  Mistakes {result.Evaluation.Mistakes:P0}";
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
