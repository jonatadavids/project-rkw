using System;
using UnityEngine;

namespace RKW.Backend
{
    /// <summary>
    /// Versioned transport DTO used only to prove the M1-T05 JSON round-trip.
    /// This is not a domain object and must not grow into the player profile.
    /// </summary>
    [Serializable]
    public sealed class CloudSaveSmokePayload : IEquatable<CloudSaveSmokePayload>
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion;
        [SerializeField] private string marker;
        [SerializeField] private int sequence;

        private CloudSaveSmokePayload()
        {
        }

        public CloudSaveSmokePayload(string marker, int sequence)
        {
            if (string.IsNullOrWhiteSpace(marker))
            {
                throw new ArgumentException("A smoke-test marker is required.", nameof(marker));
            }

            schemaVersion = CurrentSchemaVersion;
            this.marker = marker;
            this.sequence = sequence;
        }

        public int SchemaVersion => schemaVersion;
        public string Marker => marker;
        public int Sequence => sequence;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static CloudSaveSmokePayload FromJson(string json)
        {
            CloudPersistenceValidation.RequiredJson(json);

            var value = JsonUtility.FromJson<CloudSaveSmokePayload>(json);
            if (value == null || value.schemaVersion != CurrentSchemaVersion ||
                string.IsNullOrWhiteSpace(value.marker))
            {
                throw new FormatException("The Cloud Save smoke payload is invalid or unsupported.");
            }

            return value;
        }

        public bool Equals(CloudSaveSmokePayload other)
        {
            return other != null &&
                   schemaVersion == other.schemaVersion &&
                   string.Equals(marker, other.marker, StringComparison.Ordinal) &&
                   sequence == other.sequence;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CloudSaveSmokePayload);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = schemaVersion;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(marker ?? string.Empty);
                hash = (hash * 397) ^ sequence;
                return hash;
            }
        }
    }
}
