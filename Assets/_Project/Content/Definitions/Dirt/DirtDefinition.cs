using UnityEngine;

namespace CleanTime.Content
{
    /// <summary>A kind of dirt or damage (dust, stain, mold, broken furniture...). Authoring data only.</summary>
    [CreateAssetMenu(menuName = "Clean Time/Dirt Definition", fileName = "SO_Dirt_")]
    public sealed class DirtDefinition : ScriptableObject
    {
        [Tooltip("Stable business id, e.g. dirt.dust. Never rename once saves exist.")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [Min(0.01f)] [SerializeField] private float defaultHealth = 1f;
        [Min(0.01f)] [SerializeField] private float defaultWeight = 1f;

        public string Id => id;

        public string DisplayNameKey => displayNameKey;

        public float DefaultHealth => defaultHealth;

        public float DefaultWeight => defaultWeight;
    }
}
