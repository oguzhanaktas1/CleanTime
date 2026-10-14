using System;
using System.Text;

namespace CleanTime.Shared.Kernel
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5,
    }

    public interface ILogger
    {
        void Trace(string message);

        void Debug(string message);

        void Info(string message);

        void Warning(string message);

        void Error(string message, Exception ex = null);

        void Fatal(string message, Exception ex = null);
    }

    public sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        public void Trace(string message)
        {
        }

        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warning(string message)
        {
        }

        public void Error(string message, Exception ex = null)
        {
        }

        public void Fatal(string message, Exception ex = null)
        {
        }
    }

    public static class LoggerExtensions
    {
        /// <summary>Structured info log: <c>event=job_completed jobId=... score=0.94</c>.</summary>
        public static void Event(this ILogger logger, string eventName, params (string Key, object Value)[] fields)
        {
            var builder = new StringBuilder("event=").Append(eventName);

            foreach (var (key, value) in fields)
                builder.Append(' ').Append(key).Append('=').Append(value);

            logger.Info(builder.ToString());
        }
    }
}
