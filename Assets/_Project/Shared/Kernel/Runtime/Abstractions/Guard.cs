using System;

namespace CleanTime.Shared.Kernel
{
    public static class Guard
    {
        public static T NotNull<T>(T value, string name) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(name);

            return value;
        }

        public static string NotEmpty(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value must not be empty.", name);

            return value;
        }

        public static float Positive(float value, string name)
        {
            if (value <= 0f)
                throw new ArgumentOutOfRangeException(name, value, "Value must be greater than zero.");

            return value;
        }

        public static int NotNegative(int value, string name)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(name, value, "Value must not be negative.");

            return value;
        }
    }
}
