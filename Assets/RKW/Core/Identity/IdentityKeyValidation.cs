using System;

namespace RKW.Core.Identity
{
    internal static class IdentityKeyValidation
    {
        public static string RequiredId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A required identity ID cannot be null, empty, or whitespace.", parameterName);
            }

            return value;
        }

        public static int PositiveVersion(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "An identity version must be greater than zero.");
            }

            return value;
        }

        public static TEnum DefinedEnum<TEnum>(TEnum value, string parameterName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "An identity enum value must be defined.");
            }

            return value;
        }
    }
}
