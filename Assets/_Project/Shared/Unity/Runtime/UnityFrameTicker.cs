using System.Collections.Generic;
using UnityEngine;
using CleanTime.Shared.Kernel;

namespace CleanTime.Shared.Unity
{
    /// <summary>
    /// Single Update/FixedUpdate entry point for plain C# tickables, so systems do not each need a MonoBehaviour.
    /// Add/Remove are safe to call from inside a tick.
    /// </summary>
    public sealed class UnityFrameTicker : MonoBehaviour
    {
        private readonly List<IFrameTickable> _frame = new List<IFrameTickable>();
        private readonly List<IFixedTickable> _fixed = new List<IFixedTickable>();
        private readonly List<object> _pendingRemoval = new List<object>();

        public void Add(IFrameTickable tickable) => _frame.Add(tickable);

        public void Add(IFixedTickable tickable) => _fixed.Add(tickable);

        public void Remove(IFrameTickable tickable) => _pendingRemoval.Add(tickable);

        public void Remove(IFixedTickable tickable) => _pendingRemoval.Add(tickable);

        private void Update()
        {
            ApplyRemovals();
            var delta = Time.deltaTime;

            for (var i = 0; i < _frame.Count; i++)
                _frame[i].Tick(delta);
        }

        private void FixedUpdate()
        {
            var delta = Time.fixedDeltaTime;

            for (var i = 0; i < _fixed.Count; i++)
                _fixed[i].FixedTick(delta);
        }

        private void ApplyRemovals()
        {
            if (_pendingRemoval.Count == 0)
                return;

            foreach (var item in _pendingRemoval)
            {
                if (item is IFrameTickable frame)
                    _frame.Remove(frame);

                if (item is IFixedTickable fixedTickable)
                    _fixed.Remove(fixedTickable);
            }

            _pendingRemoval.Clear();
        }
    }
}
