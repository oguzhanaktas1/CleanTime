using System;
using UnityEngine;
using UnityEngine.Events;
using CleanTime.Audio.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Presentation
{
    public interface ICleaningProgressView
    {
        /// <param name="completed">0..1. The view decides whether to say "Clean 75%" or "Dirt Remaining 25%".</param>
        void SetProgress(float completed);
    }
}
