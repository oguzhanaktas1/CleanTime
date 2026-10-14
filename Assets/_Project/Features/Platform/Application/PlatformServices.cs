using System.Threading.Tasks;
using CleanTime.Shared.Kernel;

namespace CleanTime.Platform.Application
{
    public interface IPlatformService
    {
        string PlatformName { get; }

        string DeviceId { get; }
    }

    public interface IAchievementService
    {
        void Unlock(string achievementId);
    }

    /// <summary>Opaque cloud payload; the HTTP DTO is mapped to it in the adapter (anti-corruption layer).</summary>
    public sealed class CloudSaveData
    {
        public CloudSaveData(SaveId saveId, int schemaVersion, string payload, string updatedAtUtc, int revision)
        {
            SaveId = saveId;
            SchemaVersion = schemaVersion;
            Payload = payload ?? string.Empty;
            UpdatedAtUtc = updatedAtUtc ?? string.Empty;
            Revision = revision;
        }

        public SaveId SaveId { get; }

        public int SchemaVersion { get; }

        public string Payload { get; }

        public string UpdatedAtUtc { get; }

        /// <summary>Optimistic concurrency token; the server rejects stale uploads.</summary>
        public int Revision { get; }
    }

    public interface ICloudSaveService
    {
        Task<Result> UploadAsync(CloudSaveData data);

        Task<Result<CloudSaveData>> DownloadAsync(SaveId saveId);
    }

    public sealed class NullAchievementService : IAchievementService
    {
        public void Unlock(string achievementId)
        {
        }
    }

    /// <summary>Cloud saves are optional; offline play always works, so this fails fast with a clear code.</summary>
    public sealed class OfflineCloudSaveService : ICloudSaveService
    {
        public const string Unavailable = "platform.cloud_unavailable";

        public Task<Result> UploadAsync(CloudSaveData data) =>
            Task.FromResult(Result.Failure(Unavailable, "Cloud save is not available."));

        public Task<Result<CloudSaveData>> DownloadAsync(SaveId saveId) =>
            Task.FromResult(Result<CloudSaveData>.Failure(Unavailable, "Cloud save is not available."));
    }

    public sealed class PlatformModule : FeatureModule
    {
        public override string Name => "Platform";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IAchievementService>(_ => new NullAchievementService());
            services.RegisterSingleton<ICloudSaveService>(_ => new OfflineCloudSaveService());
        }
    }
}
