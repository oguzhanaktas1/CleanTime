using System;
using System.Collections.Generic;

namespace CleanTime.UI.Common
{
    /// <summary>Base for plain-C# presenters: collects subscriptions and releases them together.</summary>
    public abstract class PresenterBase : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        protected T Track<T>(T subscription)
            where T : IDisposable
        {
            _subscriptions.Add(subscription);
            return subscription;
        }

        public virtual void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();

            _subscriptions.Clear();
        }
    }
}
