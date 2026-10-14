using System;
using UnityEngine;
using UnityEngine.Events;
using CleanTime.Audio.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Presentation
{
    public sealed class WrongToolFeedbackView : MonoBehaviour
    {
        [SerializeField] private UnityEvent onWrongTool = new UnityEvent();

        public void Show() => onWrongTool.Invoke();
    }
}
