using System;
using System.Globalization;

namespace RKW.Physics
{
    /// <summary>
    /// Canonical comparison scope used only by the current local prototype.
    /// It deliberately contains the dimensions the prototype can prove today:
    /// track geometry signature and kart category. It is not a replacement for
    /// the complete domain LeaderboardKey required by the formal online flow.
    /// </summary>
    public sealed class PrototypeCompetitiveScope : IEquatable<PrototypeCompetitiveScope>
    {
        public string TrackSignature { get; }
        public string KartCategoryId { get; }

        public PrototypeCompetitiveScope(string trackSignature, string kartCategoryId)
        {
            TrackSignature = RequiredId(trackSignature, nameof(trackSignature));
            KartCategoryId = RequiredId(kartCategoryId, nameof(kartCategoryId));
        }

        public bool Equals(PrototypeCompetitiveScope other)
        {
            return other != null
                && string.Equals(TrackSignature, other.TrackSignature, StringComparison.Ordinal)
                && string.Equals(KartCategoryId, other.KartCategoryId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PrototypeCompetitiveScope);
        }

        /// <summary>For in-memory collections only; never use this value as a persisted identifier.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(TrackSignature) * 397)
                    ^ StringComparer.Ordinal.GetHashCode(KartCategoryId);
            }
        }

        /// <summary>
        /// Stable, collision-resistant segment for local PlayerPrefs keys.
        /// Length prefixes keep component boundaries unambiguous.
        /// </summary>
        internal string ToStorageKeySegment()
        {
            return "t" + TrackSignature.Length.ToString(CultureInfo.InvariantCulture) + ":" + TrackSignature
                + "|k" + KartCategoryId.Length.ToString(CultureInfo.InvariantCulture) + ":" + KartCategoryId;
        }

        private static string RequiredId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity values are required.", parameterName);
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Identity values cannot have leading or trailing whitespace.", parameterName);
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    throw new ArgumentException("Identity values cannot contain control characters.", parameterName);
                }
            }

            return value;
        }
    }
}
