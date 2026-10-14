namespace CleanTime.Shared.Kernel
{
    /// <summary>Versioned JSON payload (de)serialization, e.g. for active job state or quest progress.</summary>
    public interface IPayloadSerializer
    {
        string Serialize<T>(T value);

        T Deserialize<T>(string payload);
    }
}
