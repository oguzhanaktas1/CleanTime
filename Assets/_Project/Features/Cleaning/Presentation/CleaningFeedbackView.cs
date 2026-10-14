using System;
using UnityEngine;
using UnityEngine.Events;
using CleanTime.Audio.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>Designer hooks (particles, screen shake...) for cleaning feedback; add listeners in the inspector.</summary>
    public sealed class CleaningFeedbackView : MonoBehaviour
    {
        [SerializeField] private UnityEvent onHit = new UnityEvent();
        [SerializeField] private UnityEvent onTargetCompleted = new UnityEvent();
        [SerializeField] private UnityEvent onRoomCompleted = new UnityEvent();

        public void PlayHit() => onHit.Invoke();

        public void PlayTargetCompleted() => onTargetCompleted.Invoke();

        public void PlayRoomCompleted() => onRoomCompleted.Invoke();
    }
}
