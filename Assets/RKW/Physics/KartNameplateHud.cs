using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Rodada 46 (2026-09-01) founder request: "nos kart seria legal ter o
    /// nome em cima do carrinho na hora da corrida, até mesmo o fantasma"
    /// -- draws each kart's name as a floating OnGUI label above it,
    /// converting world position to screen space every frame via
    /// Camera.WorldToScreenPoint (the standard OnGUI world-space-label
    /// technique; see KartNameplateLayoutMath for the testable math behind
    /// the actual Rect placement). This prototype already draws every
    /// other HUD element (RaceStandingsHud, CheckpointSplitHud, TimingHUD)
    /// through OnGUI, so this follows the same approach rather than
    /// introducing a separate Canvas/world-space UI system just for this.
    ///
    /// Colors follow the same convention RaceStandingsHud's live panel
    /// already uses: green for the player, a light gray for bots. The
    /// ghost gets the same light-blue tint already painted on its own
    /// model (see KartPhysicsPrototypeBootstrap.GhostTintColor), so the
    /// label reads as "belonging" to that kart.
    ///
    /// Uses Camera.main rather than a passed-in Camera reference:
    /// KartPhysicsPrototypeBootstrap.CreateCamera tags its single race
    /// camera "MainCamera" already, but that Camera reference is a local
    /// variable inside BeginRace() and is not in scope where this
    /// component gets configured (OnRaceSetupConfirmed, further down the
    /// same class, alongside RaceStandingsHud/GhostController) -- so
    /// Camera.main is the simplest correct source here, and this
    /// prototype only ever has the one race camera active at a time
    /// either way (CreateCamera destroys any pre-existing Camera before
    /// creating its own).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartNameplateHud : MonoBehaviour
    {
        private const float HeightOffset = 1.1f;

        private readonly struct Nameplate
        {
            public readonly Transform Target;
            // Null means "always visible while Target != null" (the
            // player). Non-null means "only visible while this GameObject
            // is active in the hierarchy" -- bots and the ghost both get
            // SetActive(false)'d by existing code (SpawnBots'/
            // GhostController's own logic) at points where their label
            // should disappear too.
            public readonly GameObject VisibilityGameObject;
            public readonly string Name;
            public readonly Color Color;

            public Nameplate(Transform target, GameObject visibilityGameObject, string name, Color color)
            {
                Target = target;
                VisibilityGameObject = visibilityGameObject;
                Name = name;
                Color = color;
            }
        }

        private static readonly Color PlayerColor = new Color(0.35f, 0.95f, 0.4f);
        private static readonly Color BotColor = new Color(0.9f, 0.9f, 0.9f);
        // Same value as KartPhysicsPrototypeBootstrap.GhostTintColor --
        // duplicated here (rather than made public there) so this HUD
        // class doesn't need a compile-time dependency on that private
        // bootstrap constant; see this class's own doc comment above.
        private static readonly Color GhostColor = new Color(0.85f, 0.95f, 1f);

        private readonly List<Nameplate> _nameplates = new List<Nameplate>();
        private GUIStyle _style;

        public void Configure(Transform playerTransform, string playerName,
            IEnumerable<KartBotController> bots, Transform ghostVisual)
        {
            _nameplates.Clear();

            if (playerTransform != null)
            {
                var name = string.IsNullOrEmpty(playerName) ? "Piloto" : playerName;
                _nameplates.Add(new Nameplate(playerTransform, null, name, PlayerColor));
            }

            if (bots != null)
            {
                foreach (var bot in bots)
                {
                    if (bot == null)
                    {
                        continue;
                    }

                    _nameplates.Add(new Nameplate(bot.transform, bot.gameObject, bot.name, BotColor));
                }
            }

            if (ghostVisual != null)
            {
                _nameplates.Add(new Nameplate(ghostVisual, ghostVisual.gameObject, "Fantasma", GhostColor));
            }
        }

        private void OnGUI()
        {
            if (_nameplates.Count == 0)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            EnsureStyle();

            var scale = Mathf.Max(1f, Screen.height / 720f);

            foreach (var plate in _nameplates)
            {
                if (plate.Target == null)
                {
                    continue;
                }

                if (plate.VisibilityGameObject != null && !plate.VisibilityGameObject.activeInHierarchy)
                {
                    continue;
                }

                var worldPos = plate.Target.position + Vector3.up * HeightOffset;
                var screenPoint = camera.WorldToScreenPoint(worldPos);
                if (KartNameplateLayoutMath.IsBehindCamera(screenPoint))
                {
                    continue;
                }

                var rect = KartNameplateLayoutMath.ComputeLabelRect(screenPoint, Screen.height, scale);

                var previousColor = _style.normal.textColor;
                _style.normal.textColor = plate.Color;
                GUI.Label(rect, plate.Name, _style);
                _style.normal.textColor = previousColor;
            }
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
