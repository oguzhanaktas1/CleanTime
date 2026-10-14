using System;
using UnityEngine;
using CleanTime.GameInput.Application;
using CleanTime.Interaction.Domain;
using CleanTime.Player.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;
using CleanTime.Equipment.Application;

namespace CleanTime.Interaction.Presentation
{
    /// <summary>Picks the nearest interactable around the player; cached results, no per-frame allocations.</summary>
    public sealed class InteractionTargetSelector : MonoBehaviour
    {
        private static readonly Collider2D[] Hits = new Collider2D[16];

        [SerializeField] private float radius = 1.1f;
        [SerializeField] private LayerMask layers = ~0;

        private ContactFilter2D _filter;

        public IInteractable Current { get; private set; }

        public event Action<IInteractable> CurrentChanged;

        private void Awake()
        {
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = layers, useTriggers = true };
        }

        private void Update()
        {
            var count = Physics2D.OverlapCircle(transform.position, radius, _filter, Hits);
            IInteractable best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var interactable = Hits[i].GetComponentInParent<InteractableBehaviour>();
                if (interactable == null)
                    continue;

                var distance = ((Vector2)Hits[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = interactable;
                }
            }

            if (!ReferenceEquals(best, Current))
            {
                Current = best;
                CurrentChanged?.Invoke(best);
            }
        }
    }
}
