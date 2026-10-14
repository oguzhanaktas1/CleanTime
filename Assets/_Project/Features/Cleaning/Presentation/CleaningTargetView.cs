using UnityEngine;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>Visual side of one cleaning target. Owns sprites and feedback, never cleaning rules.</summary>
    public sealed class CleaningTargetView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;

        public string TargetInstanceId { get; private set; } = "";

        public void Initialize(string targetInstanceId)
        {
            TargetInstanceId = targetInstanceId;
        }

        public void SetProgress(float normalizedProgress)
        {
            if (targetRenderer == null)
                return;

            normalizedProgress = Mathf.Clamp01(normalizedProgress);

            var color = targetRenderer.color;
            color.a = 1f - normalizedProgress;
            targetRenderer.color = color;
        }

        public void PlayCompletedFeedback()
        {
            gameObject.SetActive(false);
        }
    }
}
