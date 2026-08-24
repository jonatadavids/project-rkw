using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Small procedurally-generated UI textures for the prototype HUD
    /// (pedal icons, steering wheel, starter's checkered bar). Generated at
    /// runtime instead of importing external art so there is nothing to
    /// license (AGENTS.md rule 10) and nothing that can go missing on a
    /// fresh checkout. Callers own the returned Texture2D and should
    /// Destroy() it when done (see the OnDestroy pattern already used by
    /// RKW.Audio.AudioValidationHarness for its runtime AudioClips).
    ///
    /// Round 27 (2026-08-24): <see cref="KartPrototypeInput"/> now prefers
    /// baked icon art (the founder's own modeled steering wheel/pedal box,
    /// rendered to PNG and loaded via Resources.Load) when present, and
    /// only falls back to these procedural silhouettes if that asset is
    /// missing. This keeps the "nothing that can go missing on a fresh
    /// checkout" guarantee this class was built for, while allowing the
    /// nicer baked art to be the normal case.
    /// </summary>
    internal static class ProceduralUITextures
    {
        /// <summary>Black/white checkered bar (starter's flag look) for the race-start sequence.</summary>
        internal static Texture2D CreateCheckerTexture(int squareSizePixels, int squaresPerAxis)
        {
            var size = Mathf.Max(2, squareSizePixels * squaresPerAxis);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var checkerX = x / Mathf.Max(1, squareSizePixels);
                    var checkerY = y / Mathf.Max(1, squareSizePixels);
                    var isBlack = (checkerX + checkerY) % 2 == 0;
                    texture.SetPixel(x, y, isBlack ? Color.black : Color.white);
                }
            }

            texture.Apply();
            return texture;
        }

        /// <summary>
        /// A simple steering-wheel silhouette (rim + hub + 3 spokes) so
        /// rotation reads clearly — a plain ring alone looks identical at
        /// any rotation angle. Left white (not pre-colored): the caller
        /// tints it via GUI.color, and the baked replacement (round 27) is
        /// already full-color art, so this fallback only needs to match its
        /// alpha-only tinting convention, not carry its own hue.
        /// </summary>
        internal static Texture2D CreateSteeringWheelTexture(int size)
        {
            size = Mathf.Max(8, size);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var center = (size - 1) / 2f;
            var outerRadius = size * 0.46f;
            var innerRadius = size * 0.36f;
            var hubRadius = size * 0.12f;
            var spokeHalfWidthPixels = size * 0.045f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var opaque = false;

                    if (distance <= hubRadius)
                    {
                        opaque = true;
                    }
                    else if (distance >= innerRadius && distance <= outerRadius)
                    {
                        opaque = true;
                    }
                    else if (distance < innerRadius && distance > hubRadius)
                    {
                        var angleDegrees = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (IsNearSpokeAngle(angleDegrees, 90f, spokeHalfWidthPixels, distance) ||
                            IsNearSpokeAngle(angleDegrees, 210f, spokeHalfWidthPixels, distance) ||
                            IsNearSpokeAngle(angleDegrees, 330f, spokeHalfWidthPixels, distance))
                        {
                            opaque = true;
                        }
                    }

                    texture.SetPixel(x, y, opaque ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return texture;
        }

        private static bool IsNearSpokeAngle(
            float angleDegrees, float spokeAngleDegrees, float halfWidthPixels, float distanceFromCenter)
        {
            var delta = Mathf.DeltaAngle(angleDegrees, spokeAngleDegrees);
            var radius = Mathf.Max(0.001f, distanceFromCenter);
            var toleranceDegrees = (halfWidthPixels / radius) * Mathf.Rad2Deg;
            return Mathf.Abs(delta) <= toleranceDegrees;
        }

        /// <summary>
        /// A pedal-pad silhouette: narrow at the top (pivot), wide at the
        /// bottom (foot contact). Texture row 0 is the bottom of the image
        /// when drawn with GUI.DrawTexture (Unity's standard, non-flipped
        /// texture-to-rect mapping).
        ///
        /// Round 27 (2026-08-24): <paramref name="color"/> is now baked
        /// directly into the opaque pixels instead of being left to the
        /// caller's GUI.color tint. This is a deliberate switch from the
        /// original design: the caller previously shared ONE white pedal
        /// texture for both brake and throttle, tinting it red/green at
        /// draw time via GUI.color's RGB channels. That approach conflicts
        /// with the new baked-art icons (round 27) — those are full-color
        /// renders of the founder's real pedal box, and hue-multiplying
        /// them with GUI.color would muddy their true colors. Baking the
        /// tint in here instead lets the caller switch to alpha-only
        /// GUI.color tinting (for intensity fade) universally, whether the
        /// icon in use is this procedural fallback or the baked art.
        ///
        /// Round 28 (2026-08-24) founder feedback: "pode tirar aquele
        /// verde e vermelho a animação do pedal já ajuda" — the caller now
        /// always passes a single neutral gray (see
        /// KartPrototypeInput.PedalNeutralColor) instead of a red/green
        /// pair; <paramref name="color"/> stays a parameter (not hardcoded
        /// here) so the caller still owns the actual color choice.
        /// </summary>
        /// <summary>
        /// Round 28 (2026-08-24) founder feedback (relayed from a
        /// first-time player trying the game): "senti que ela ficou
        /// confusa de onde apertar... seria legal ele aparecer mais
        /// evidente". A soft dark radial glow drawn behind the wheel/pedal
        /// icons so they stand out from a busy 3D track background
        /// instead of blending into it — the same trick a lot of mobile
        /// HUDs use instead of a hard-edged box (which would look like a
        /// button the icon "sits inside" rather than glows above).
        /// </summary>
        internal static Texture2D CreateHudBackingTexture(int size)
        {
            size = Mathf.Max(8, size);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var center = (size - 1) / 2f;
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var t = Mathf.Clamp01(distance / radius);
                    var alpha = Mathf.SmoothStep(0.6f, 0f, t);
                    texture.SetPixel(x, y, new Color(0.02f, 0.02f, 0.04f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        internal static Texture2D CreatePedalTexture(int width, int height, Color color)
        {
            width = Mathf.Max(4, width);
            height = Mathf.Max(4, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var opaqueColor = new Color(color.r, color.g, color.b, 1f);
            var center = width * 0.5f;
            for (var y = 0; y < height; y++)
            {
                var bottomToTop = height <= 1 ? 0f : y / (float)(height - 1);
                var halfWidth = Mathf.Lerp(width * 0.46f, width * 0.20f, bottomToTop);
                for (var x = 0; x < width; x++)
                {
                    var opaque = Mathf.Abs(x - center) <= halfWidth;
                    texture.SetPixel(x, y, opaque ? opaqueColor : Color.clear);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
