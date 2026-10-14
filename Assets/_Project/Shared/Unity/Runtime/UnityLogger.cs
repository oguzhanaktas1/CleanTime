using System;
using UnityEngine;
using CleanTime.Shared.Kernel;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Shared.Unity
{
    /// <summary>Routes Kernel logging to the Unity console. Verbose levels are filtered by <see cref="MinimumLevel"/>.</summary>
    public sealed class UnityLogger : KernelLogger
    {
        private const string Prefix = "[CleanTime] ";

        public UnityLogger(LogLevel minimumLevel = LogLevel.Debug)
        {
            MinimumLevel = minimumLevel;
        }

        public LogLevel MinimumLevel { get; set; }

        public void Trace(string message)
        {
            if (Enabled(LogLevel.Trace))
                UnityEngine.Debug.Log(Prefix + message);
        }

        public void Debug(string message)
        {
            if (Enabled(LogLevel.Debug))
                UnityEngine.Debug.Log(Prefix + message);
        }

        public void Info(string message)
        {
            if (Enabled(LogLevel.Info))
                UnityEngine.Debug.Log(Prefix + message);
        }

        public void Warning(string message)
        {
            if (Enabled(LogLevel.Warning))
                UnityEngine.Debug.LogWarning(Prefix + message);
        }

        public void Error(string message, Exception ex = null)
        {
            if (!Enabled(LogLevel.Error))
                return;

            UnityEngine.Debug.LogError(Prefix + message);

            if (ex != null)
                UnityEngine.Debug.LogException(ex);
        }

        public void Fatal(string message, Exception ex = null) => Error("FATAL: " + message, ex);

        private bool Enabled(LogLevel level) => level >= MinimumLevel;
    }
}
