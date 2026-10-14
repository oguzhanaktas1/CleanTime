// Unity's C# 9 toolchain ships without this type, which `init` accessors and records require.
// It is public so every assembly that references the Kernel can use records.
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}
