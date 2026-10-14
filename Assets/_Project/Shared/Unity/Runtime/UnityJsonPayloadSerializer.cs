using UnityEngine;
using CleanTime.Shared.Kernel;

namespace CleanTime.Shared.Unity
{
    /// <summary>JsonUtility based payload serializer. Payload types must be [Serializable] classes.</summary>
    public sealed class UnityJsonPayloadSerializer : IPayloadSerializer
    {
        public string Serialize<T>(T value) => JsonUtility.ToJson(value);

        public T Deserialize<T>(string payload) => JsonUtility.FromJson<T>(payload);
    }
}
