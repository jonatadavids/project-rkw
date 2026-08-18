using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RKW.Audio
{
    /// <summary>
    /// Isolated device-validation harness. Synthetic sounds and controls are technical
    /// fixtures only and are not the game's final audio system or content.
    /// </summary>
    public sealed class AudioValidationHarness : MonoBehaviour
    {
        private const string MixerResourceName = "RKWAudioValidation";

        [SerializeField] private AudioMixer audioMixer;

        private readonly Dictionary<AudioValidationLayer, AudioSource> _sources =
            new Dictionary<AudioValidationLayer, AudioSource>();
        private readonly List<AudioClip> _runtimeClips = new List<AudioClip>();
        private float _engineTargetVolume = AudioValidationConfiguration.EngineVolume;
        private float _engineTargetPitch = 1f;
        private float _orbitTime;
        private bool _impactEnabled = true;

        internal bool IsInitialized { get; private set; }
        internal bool LoopsRequested { get; private set; }
        internal int ImpactTriggerCount { get; private set; }
        internal IReadOnlyDictionary<AudioValidationLayer, AudioSource> Sources => _sources;

        private void Awake()
        {
            Initialize();
            StartLoops();
        }

        private void Update()
        {
            AdvanceEngineSmoothing(Time.unscaledDeltaTime);
            AdvanceSourceOrbit(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            foreach (var clip in _runtimeClips)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }
            }

            _runtimeClips.Clear();
        }

        internal void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (audioMixer == null)
            {
                audioMixer = Resources.Load<AudioMixer>(MixerResourceName);
            }

            if (audioMixer == null)
            {
                throw new InvalidOperationException("Audio validation mixer is unavailable.");
            }

            CreateSource(AudioValidationLayer.Engine, "Engine", true, 0.65f);
            CreateSource(AudioValidationLayer.Road, "TiresAndRoad", true, 0.55f);
            CreateSource(AudioValidationLayer.Impact, "Impacts", false, 0f);
            CreateSource(AudioValidationLayer.Ambience, "Ambience", true, 0f);
            IsInitialized = true;
        }

        internal void StartLoops()
        {
            Initialize();
            LoopsRequested = true;
            PlayLoop(AudioValidationLayer.Engine);
            PlayLoop(AudioValidationLayer.Road);
            PlayLoop(AudioValidationLayer.Ambience);
        }

        internal void StopAll()
        {
            foreach (var source in _sources.Values)
            {
                source.Stop();
            }

            LoopsRequested = false;
        }

        internal void RestartLoops()
        {
            StopAll();
            StartLoops();
        }

        internal void SetLayerEnabled(AudioValidationLayer layer, bool enabled)
        {
            Initialize();
            if (layer == AudioValidationLayer.Impact)
            {
                _impactEnabled = enabled;
                return;
            }

            _sources[layer].mute = !enabled;
        }

        internal bool IsLayerEnabled(AudioValidationLayer layer)
        {
            return layer == AudioValidationLayer.Impact
                ? _impactEnabled
                : !_sources[layer].mute;
        }

        internal void TriggerImpact()
        {
            Initialize();
            if (!_impactEnabled)
            {
                return;
            }

            var source = _sources[AudioValidationLayer.Impact];
            source.PlayOneShot(source.clip, 1f);
            ImpactTriggerCount++;
        }

        internal void SetEngineTargets(float volume, float pitch)
        {
            _engineTargetVolume = Mathf.Clamp(volume, 0f, AudioValidationConfiguration.EngineVolume);
            _engineTargetPitch = Mathf.Clamp(
                pitch,
                AudioValidationConfiguration.MinimumEnginePitch,
                AudioValidationConfiguration.MaximumEnginePitch);
        }

        internal void AdvanceEngineSmoothing(float deltaTime)
        {
            if (!IsInitialized || deltaTime <= 0f)
            {
                return;
            }

            var engine = _sources[AudioValidationLayer.Engine];
            engine.volume = Mathf.MoveTowards(
                engine.volume,
                _engineTargetVolume,
                AudioValidationConfiguration.EngineVolumeChangePerSecond * deltaTime);
            engine.pitch = Mathf.MoveTowards(
                engine.pitch,
                _engineTargetPitch,
                AudioValidationConfiguration.EnginePitchChangePerSecond * deltaTime);
        }

        private void CreateSource(AudioValidationLayer layer, string mixerGroupName, bool loop, float spatialBlend)
        {
            var child = new GameObject($"Synthetic {layer}");
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            var clip = ProceduralAudioFactory.Create(layer);
            _runtimeClips.Add(clip);
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = AudioValidationConfiguration.SafeVolume(layer);
            source.pitch = 1f;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.minDistance = 1f;
            source.maxDistance = 12f;
            source.priority = layer == AudioValidationLayer.Impact ? 32 : 128;
            source.outputAudioMixerGroup = AudioValidationConfiguration.FindRequiredGroup(audioMixer, mixerGroupName);
            _sources.Add(layer, source);
        }

        private void PlayLoop(AudioValidationLayer layer)
        {
            var source = _sources[layer];
            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        private void AdvanceSourceOrbit(float deltaTime)
        {
            if (!IsInitialized)
            {
                return;
            }

            _orbitTime += deltaTime;
            _sources[AudioValidationLayer.Engine].transform.localPosition = new Vector3(
                Mathf.Cos(_orbitTime * 0.7f) * 2.5f,
                0f,
                Mathf.Sin(_orbitTime * 0.7f) * 2.5f);
            _sources[AudioValidationLayer.Road].transform.localPosition = new Vector3(
                Mathf.Cos(-_orbitTime * 0.45f) * 1.8f,
                0f,
                Mathf.Sin(-_orbitTime * 0.45f) * 1.8f);
        }

        private void OnGUI()
        {
            if (!IsInitialized)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var scale = Mathf.Max(0.5f, Mathf.Min(safeArea.width / 960f, safeArea.height / 540f));
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(safeArea.xMin, safeArea.yMin, 0f),
                Quaternion.identity,
                Vector3.one * scale);

            GUILayout.BeginArea(new Rect(20f, 14f, (safeArea.width / scale) - 40f, (safeArea.height / scale) - 28f));
            GUILayout.Label("PROJECT RKW • TESTE TÉCNICO DE ÁUDIO");
            GUILayout.Label("Sons sintéticos provisórios — volume inicial seguro");

            GUILayout.BeginHorizontal();
            DrawLayerToggle(AudioValidationLayer.Engine, "MOTOR");
            DrawLayerToggle(AudioValidationLayer.Road, "ZEBRA / ROLAGEM");
            DrawLayerToggle(AudioValidationLayer.Ambience, "AMBIENTE");
            DrawLayerToggle(AudioValidationLayer.Impact, "COLISÃO");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("INICIAR CAMADAS", GUILayout.Height(54f)))
            {
                StartLoops();
            }
            if (GUILayout.Button("PARAR TUDO", GUILayout.Height(54f)))
            {
                StopAll();
            }
            if (GUILayout.Button("REPETIR", GUILayout.Height(54f)))
            {
                RestartLoops();
            }
            if (GUILayout.Button("DISPARAR COLISÃO", GUILayout.Height(54f)))
            {
                TriggerImpact();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Volume motor: {_engineTargetVolume:F2}");
            SetEngineTargets(GUILayout.HorizontalSlider(_engineTargetVolume, 0f, AudioValidationConfiguration.EngineVolume), _engineTargetPitch);
            GUILayout.Label($"Pitch motor: {_engineTargetPitch:F2}");
            SetEngineTargets(_engineTargetVolume, GUILayout.HorizontalSlider(_engineTargetPitch, AudioValidationConfiguration.MinimumEnginePitch, AudioValidationConfiguration.MaximumEnginePitch));

            AudioSettings.GetDSPBufferSize(out var bufferLength, out var bufferCount);
            GUILayout.Label($"Estado técnico: {AudioSettings.outputSampleRate} Hz • DSP {bufferLength} × {bufferCount} • colisões {ImpactTriggerCount}");
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private void DrawLayerToggle(AudioValidationLayer layer, string label)
        {
            var enabled = GUILayout.Toggle(IsLayerEnabled(layer), label, GUILayout.Height(42f));
            if (enabled != IsLayerEnabled(layer))
            {
                SetLayerEnabled(layer, enabled);
            }
        }
    }
}
