using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RKW.Backend
{
    internal static class CloudPersistenceValidation
    {
        internal const int MaximumJsonBytes = 32 * 1024;

        internal static string RequiredKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A Cloud Save key is required.", nameof(key));
            }

            if (char.IsWhiteSpace(key[0]) || char.IsWhiteSpace(key[key.Length - 1]))
            {
                throw new ArgumentException(
                    "Cloud Save keys cannot have leading or trailing whitespace.",
                    nameof(key));
            }

            foreach (var character in key)
            {
                if (char.IsControl(character))
                {
                    throw new ArgumentException(
                        "Cloud Save keys cannot contain control characters.",
                        nameof(key));
                }
            }

            return key;
        }

        internal static string RequiredJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("A JSON payload is required.", nameof(json));
            }

            var byteCount = Encoding.UTF8.GetByteCount(json);
            if (byteCount > MaximumJsonBytes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(json),
                    $"The JSON payload exceeds the project budget of {MaximumJsonBytes} bytes.");
            }

            try
            {
                JToken.Parse(json);
            }
            catch (JsonReaderException exception)
            {
                throw new ArgumentException(
                    "The payload must contain syntactically valid JSON.",
                    nameof(json),
                    exception);
            }

            return json;
        }
    }
}
