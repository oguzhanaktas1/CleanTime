using System;
using System.Collections.Generic;

namespace CleanTime.Shared.Kernel
{
    public static class Empty<T>
    {
        public static readonly IReadOnlyList<T> List = Array.Empty<T>();
    }
}
