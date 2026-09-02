using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 44 (2026-09-01) founder feedback: "vc pode colocar checkpoint
    /// sei la dividir a pista em 3 colocar uma lista e informar naquele
    /// ponto vc foi 1 segundo a mais ou menos da ultima volta acho que fica
    /// legal". Listens to TimingManagerLite.OnCheckpointSplit (which fires
    /// off the checkpoint triggers that already exist for lap validation --
    /// see TimingManagerLite's own doc comment) and shows a persistent
    /// 3-row list ("SETOR 1/2/3"), each with the time it took to reach that
    /// point this lap and how that compares to the same point on the last
    /// VALID lap (green "-0.34s" = faster, red "+0.34s" = slower, gray
    /// "--" before it has anything to compare against yet).
    ///
    /// Round 45 (2026-09-01) founder feedback: the first version of this
    /// panel was anchored top-left, which turned out to already be used by
    /// CameraViewToggleButton and (stacked under it) KartCategoryToggleButton
    /// — "os setores... ficaram sobrepondo a mudanca de camera e a escolha
    /// do kart". Moved to top-CENTER instead, stacked directly below
    /// KartPrototypeInput's speed readout.
    ///
    /// Round 46 (2026-09-01) founder feedback: "vc centralizou nao ficou
    /// bom! vc poderia alinhar com a classificacao porem a esquerda, do
    /// outro lado" -- centered didn't read well either, so this now
    /// mirrors RaceStandingsHud's (CLASSIFICAÇÃO) panel exactly: same
    /// HudLayoutMath.ComputeStandingsPanelTop Y position, same 12px margin,
    /// just anchored to the LEFT safe-area edge instead of the right. That
    /// Y is comfortably below CameraViewToggleButton/KartCategoryToggleButton
    /// (which only occupy the first ~100px of the top-left corner, pre-race
    /// only for the second one), so this does not reopen the round-45
    /// overlap.
    ///
    /// Also new this round: "vc pode colocar o tempo no meio da tela quando
    /// fazer o checkpoint vermelho ou verde e depois sumir e continuar
    /// gravando na esquerda" -- a second, separate readout: a big
    /// transient label in the middle of the screen the instant a
    /// checkpoint is crossed (green if faster than the same point last
    /// lap, red if slower, gray on the first lap when there is nothing to
    /// compare against yet), which fades out after a couple of seconds --
    /// same fade-out pattern as RaceManager's own per-lap toast -- while
    /// the persistent SETORES list on the left keeps recording every
    /// split regardless.
    ///
    /// Round 46, second pass, same day -- founder feedback: "gostei da
    /// pegada de aparecer o tempo mas acho que aparece muitas vezes,
    /// poderia diminuir um pouco a quantidade". The RAW checkpoint
    /// triggers this listens to (TimingManagerLite.OnCheckpointSplit) fire
    /// once per physical checkpoint object on the track -- 3 on the oval,
    /// but 16 on Circuit2 (see SectorSplitMath's own doc comment) -- while
    /// this HUD only ever shows 3 SECTORS. The persistent list on the left
    /// is fine either way (each sector row just keeps getting overwritten
    /// until you leave that sector), but the center toast was popping up
    /// on EVERY raw checkpoint, which is genuinely excessive on Circuit2.
    /// Throttled to fire only once per sector per lap -- the first raw
    /// checkpoint that lands in a given sector -- matching the 3-row list
    /// it summarizes instead of the raw checkpoint count underneath it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointSplitHud : MonoBehaviour
    {
        private const int SectorCount = 3;
        private const float RecentSplitHighlightSeconds = 3f;
        // Round 46: how long the big center-screen checkpoint toast stays
        // up before fading -- shorter than RaceManager's 2.6s lap toast
        // since checkpoints fire much more often than laps and a slower
        // fade would risk overlapping the NEXT checkpoint's toast on a
        // short sector.
        private const float CenterToastDurationSeconds = 1.6f;

        private TimingManagerLite _timing;
        private int _totalCheckpoints = 1;

        private readonly float[] _sectorSplitSeconds = new float[SectorCount];
        private readonly float[] _sectorDeltaSeconds = new float[SectorCount];
        private readonly bool[] _sectorHasDelta = new bool[SectorCount];
        private readonly bool[] _sectorHasSplit = new bool[SectorCount];
        private readonly float[] _sectorUpdatedAtUnscaledTime = new float[SectorCount];

        private string _centerToastText;
        private bool _centerToastHasDelta;
        private float _centerToastDeltaSeconds;
        private float _centerToastShownUntilUnscaledTime = -1f;

        private GUIStyle _titleStyle;
        private GUIStyle _neutralRowStyle;
        private GUIStyle _fasterRowStyle;
        private GUIStyle _slowerRowStyle;
        private GUIStyle _centerToastNeutralStyle;
        private GUIStyle _centerToastFasterStyle;
        private GUIStyle _centerToastSlowerStyle;

        public void Configure(TimingManagerLite timing, int totalCheckpoints)
        {
            if (_timing != null)
            {
                _timing.OnCheckpointSplit -= HandleCheckpointSplit;
                _timing.OnLapCompleted -= HandleLapBoundary;
                _timing.OnLapInvalidated -= HandleLapInvalidatedBoundary;
            }

            _timing = timing;
            _totalCheckpoints = Mathf.Max(1, totalCheckpoints);
            ResetSectorDisplay();

            if (_timing != null)
            {
                _timing.OnCheckpointSplit += HandleCheckpointSplit;
                // Either outcome means a new lap (and a fresh set of
                // sector splits) is about to start -- see
                // TimingManagerLite.HandleStartFinishCrossing, which always
                // calls StartNewLap() right after firing one of these.
                _timing.OnLapCompleted += HandleLapBoundary;
                _timing.OnLapInvalidated += HandleLapInvalidatedBoundary;
            }
        }

        private void OnDestroy()
        {
            if (_timing != null)
            {
                _timing.OnCheckpointSplit -= HandleCheckpointSplit;
                _timing.OnLapCompleted -= HandleLapBoundary;
                _timing.OnLapInvalidated -= HandleLapInvalidatedBoundary;
            }
        }

        private void HandleCheckpointSplit(int checkpointIndex, float splitSeconds, float? deltaVsPreviousLap)
        {
            var sector = SectorSplitMath.ComputeSectorIndex(checkpointIndex, _totalCheckpoints, SectorCount);
            // Round 46, second pass: this is the FIRST raw checkpoint of
            // this sector this lap exactly when _sectorHasSplit[sector] is
            // still false from the last ResetSectorDisplay -- check it
            // BEFORE overwriting the row below, since that's what flips it
            // to true.
            var isFirstHitInSectorThisLap = !_sectorHasSplit[sector];

            _sectorSplitSeconds[sector] = splitSeconds;
            _sectorHasSplit[sector] = true;
            _sectorHasDelta[sector] = deltaVsPreviousLap.HasValue;
            _sectorDeltaSeconds[sector] = deltaVsPreviousLap ?? 0f;
            _sectorUpdatedAtUnscaledTime[sector] = Time.unscaledTime;

            if (!isFirstHitInSectorThisLap)
            {
                return;
            }

            // Round 46: the transient center-screen readout -- see class
            // doc. Uses Time.unscaledTime (not Time.time) so the toast
            // still fades on schedule even if PauseButton has frozen
            // Time.timeScale (matches RaceManager's own toast, which uses
            // the same convention).
            _centerToastHasDelta = deltaVsPreviousLap.HasValue;
            _centerToastDeltaSeconds = deltaVsPreviousLap ?? 0f;
            _centerToastText = deltaVsPreviousLap.HasValue
                ? FormatDeltaLabel(deltaVsPreviousLap.Value)
                : FormatSeconds(splitSeconds);
            _centerToastShownUntilUnscaledTime = Time.unscaledTime + CenterToastDurationSeconds;
        }

        private static string FormatDeltaLabel(float delta)
        {
            return delta <= 0f ? $"-{FormatSeconds(-delta)}" : $"+{FormatSeconds(delta)}";
        }

        private void HandleLapBoundary(float lapTime, bool isValid)
        {
            ResetSectorDisplay();
        }

        private void HandleLapInvalidatedBoundary()
        {
            ResetSectorDisplay();
        }

        private void ResetSectorDisplay()
        {
            for (var i = 0; i < SectorCount; i++)
            {
                _sectorSplitSeconds[i] = 0f;
                _sectorDeltaSeconds[i] = 0f;
                _sectorHasDelta[i] = false;
                _sectorHasSplit[i] = false;
                _sectorUpdatedAtUnscaledTime[i] = -1f;
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _titleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

            _neutralRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                alignment = TextAnchor.MiddleLeft
            };
            _neutralRowStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            _fasterRowStyle = new GUIStyle(_neutralRowStyle);
            _fasterRowStyle.normal.textColor = new Color(0.4f, 0.95f, 0.45f);

            _slowerRowStyle = new GUIStyle(_neutralRowStyle);
            _slowerRowStyle.normal.textColor = new Color(0.95f, 0.4f, 0.4f);

            _centerToastNeutralStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(46f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _centerToastNeutralStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _centerToastFasterStyle = new GUIStyle(_centerToastNeutralStyle);
            _centerToastFasterStyle.normal.textColor = new Color(0.35f, 0.95f, 0.4f);

            _centerToastSlowerStyle = new GUIStyle(_centerToastNeutralStyle);
            _centerToastSlowerStyle.normal.textColor = new Color(0.95f, 0.3f, 0.3f);
        }

        private void OnGUI()
        {
            if (_timing == null)
            {
                return;
            }

            EnsureStyles();

            var scale = Mathf.Max(1f, Screen.height / 720f);
            var rowHeight = 22f * scale;
            var panelWidth = 260f * scale;
            var panelHeight = (SectorCount + 1) * rowHeight + 8f * scale;
            // Round 46 (see class doc): mirrors RaceStandingsHud's own
            // panelRect exactly -- same HudLayoutMath Y, same 12px margin
            // -- just anchored to the left safe-area edge instead of
            // "Screen.width - panelWidth - 12*scale" on the right.
            var panelTop = HudLayoutMath.ComputeStandingsPanelTop(scale, Screen.safeArea.yMin, defaultTopPixels: 80f);
            var panelRect = new Rect(Screen.safeArea.xMin + 12f * scale, panelTop, panelWidth, panelHeight);

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            var y = panelRect.y + 4f * scale;
            GUI.Label(new Rect(panelRect.x + 8f * scale, y, panelRect.width - 12f * scale, rowHeight), "SETORES", _titleStyle);
            y += rowHeight;

            for (var i = 0; i < SectorCount; i++)
            {
                var label = BuildSectorLabel(i);
                var style = SectorRowStyle(i);
                GUI.Label(new Rect(panelRect.x + 8f * scale, y, panelRect.width - 12f * scale, rowHeight), label, style);
                y += rowHeight;
            }

            DrawCenterToastIfActive(scale);
        }

        private void DrawCenterToastIfActive(float scale)
        {
            if (_centerToastShownUntilUnscaledTime < 0f || Time.unscaledTime > _centerToastShownUntilUnscaledTime)
            {
                return;
            }

            var style = !_centerToastHasDelta
                ? _centerToastNeutralStyle
                : (_centerToastDeltaSeconds <= 0f ? _centerToastFasterStyle : _centerToastSlowerStyle);

            // Fade out over the last third of the toast's lifetime -- same
            // shape as RaceManager's own lap toast fade.
            var remaining = _centerToastShownUntilUnscaledTime - Time.unscaledTime;
            var alpha = Mathf.Clamp01(remaining / (CenterToastDurationSeconds * 0.35f));
            var previousColor = style.normal.textColor;
            style.normal.textColor = new Color(previousColor.r, previousColor.g, previousColor.b, alpha);

            var toastRect = new Rect(0f, Screen.height * 0.42f, Screen.width, 70f * scale);
            GUI.Label(toastRect, _centerToastText, style);

            style.normal.textColor = previousColor;
        }

        private string BuildSectorLabel(int sectorIndex)
        {
            var name = $"SETOR {sectorIndex + 1}";
            if (!_sectorHasSplit[sectorIndex])
            {
                return $"{name}: --";
            }

            var splitLabel = FormatSeconds(_sectorSplitSeconds[sectorIndex]);
            if (!_sectorHasDelta[sectorIndex])
            {
                return $"{name}: {splitLabel}";
            }

            var delta = _sectorDeltaSeconds[sectorIndex];
            var deltaLabel = delta <= 0f
                ? $"-{FormatSeconds(-delta)}"
                : $"+{FormatSeconds(delta)}";
            return $"{name}: {splitLabel} ({deltaLabel})";
        }

        private GUIStyle SectorRowStyle(int sectorIndex)
        {
            if (!_sectorHasDelta[sectorIndex] || !_sectorHasSplit[sectorIndex])
            {
                return _neutralRowStyle;
            }

            // Only tint recently-updated rows -- otherwise a sector's color
            // from earlier in the lap would sit there unexplained the rest
            // of the way around.
            var age = Time.unscaledTime - _sectorUpdatedAtUnscaledTime[sectorIndex];
            if (age > RecentSplitHighlightSeconds)
            {
                return _neutralRowStyle;
            }

            return _sectorDeltaSeconds[sectorIndex] <= 0f ? _fasterRowStyle : _slowerRowStyle;
        }

        private static string FormatSeconds(float seconds)
        {
            return $"{seconds:0.00}s";
        }
    }
}
