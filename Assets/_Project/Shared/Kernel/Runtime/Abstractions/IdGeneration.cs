using System;

namespace CleanTime.Shared.Kernel
{
    public interface IIdGenerator
    {
        string NewId();
    }

    public sealed class GuidIdGenerator : IIdGenerator
    {
        public string NewId() => Guid.NewGuid().ToString("N");
    }
}
