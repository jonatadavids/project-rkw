using System;

namespace RKW.Backend
{
    /// <summary>
    /// Finite timeout policy for every remote operation in the UGS foundation.
    /// </summary>
    public sealed class UgsOperationTimeouts
    {
        public static UgsOperationTimeouts Default => new UgsOperationTimeouts(
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10));

        public UgsOperationTimeouts(
            TimeSpan initialization,
            TimeSpan authentication,
            TimeSpan save,
            TimeSpan load)
        {
            Initialization = RequiredFinite(initialization, nameof(initialization));
            Authentication = RequiredFinite(authentication, nameof(authentication));
            Save = RequiredFinite(save, nameof(save));
            Load = RequiredFinite(load, nameof(load));
        }

        public TimeSpan Initialization { get; }
        public TimeSpan Authentication { get; }
        public TimeSpan Save { get; }
        public TimeSpan Load { get; }

        private static TimeSpan RequiredFinite(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "UGS timeouts must be finite, positive, and supported by CancellationTokenSource.");
            }

            return value;
        }
    }
}
