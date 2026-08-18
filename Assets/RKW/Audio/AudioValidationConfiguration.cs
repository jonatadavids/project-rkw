using System;
using UnityEngine;
using UnityEngine.Audio;

namespace RKW.Audio
{
    internal enum AudioValidationLayer
    {
        Engine,
        Road,
        Impact,
        Ambience
    }

    internal static class AudioValidationConfiguration
    {
        internal const int SampleRate = 48000;
        internal const float EngineVolume = 0.18f;
        internal const float RoadVolume = 0.12f;
        internal const float ImpactVolume = 0.25f;
        internal const float AmbienceVolume = 0.08f;
        internal const float MinimumEnginePitch = 0.75f;
        internal const float MaximumEnginePitch = 1.5f;
        internal const float EngineVolumeChangePerSecond = 0.35f;
        internal const float EnginePitchChangePerSecond = 0.8f;

        internal static readonly string[] MixerGroupNames =
        {
            "Engine",
            "TiresAndRoad",
            "Impacts",
            "Ambience"
        };

        internal static float SafeVolume(AudioValidationLayer layer)
        {
            switch (layer)
            {
                case AudioValidationLayer.Engine:
                    return EngineVolume;
                case AudioValidationLayer.Road:
                    return RoadVolume;
                case AudioValidationLayer.Impact:
                    return ImpactVolume;
                case AudioValidationLayer.Ambience:
                    return AmbienceVolume;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }

        internal static AudioMixerGroup FindRequiredGroup(AudioMixer mixer, string groupName)
        {
            if (mixer == null)
            {
                throw new ArgumentNullException(nameof(mixer));
            }

            var matches = mixer.FindMatchingGroups(groupName);
            foreach (var match in matches)
            {
                if (string.Equals(match.name, groupName, StringComparison.Ordinal))
                {
                    return match;
                }
            }

            throw new InvalidOperationException($"Required audio mixer group is missing: {groupName}.");
        }
    }
}
