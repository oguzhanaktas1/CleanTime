using UnityEngine;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>Selects the closest incomplete target near the player using cached physics queries.</summary>
    public sealed class CleaningTargetSelector : MonoBehaviour
    {
        private static readonly Collider2D[] Hits = new Collider2D[32];

        [SerializeField] private float radius = 1.0f;
        [SerializeField] private LayerMask layers = ~0;

        private ContactFilter2D _filter;

        public CleaningTargetView Current { get; private set; }

        private void Awake()
        {
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = layers, useTriggers = true };
        }

        private void Update()
        {
            var count = Physics2D.OverlapCircle(transform.position, radius, _filter, Hits);
            CleaningTargetView best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var view = Hits[i].GetComponentInParent<CleaningTargetView>();
                if (view == null || !view.gameObject.activeInHierarchy)
                    continue;

                var distance = ((Vector2)view.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = view;
                }
            }

            Current = best;
        }
    }
}
