using System;

namespace CleanTime.Shared.Kernel
{
    public readonly struct Error : IEquatable<Error>
    {
        public string Code { get; }

        public string Message { get; }

        public Error(string code, string message)
        {
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Equals(Error other) => Code == other.Code && Message == other.Message;

        public override bool Equals(object obj) => obj is Error other && Equals(other);

        public override int GetHashCode() => (Code, Message).GetHashCode();

        public override string ToString() => $"{Code}: {Message}";

        public static bool operator ==(Error left, Error right) => left.Equals(right);

        public static bool operator !=(Error left, Error right) => !left.Equals(right);
    }
}
