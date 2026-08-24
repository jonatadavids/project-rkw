using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder request, 2026-08-24 (round 27): "Numeração dos Karts:
    /// Incluir numeração visível nas carenagens de todos os karts (Jogador,
    /// Bots e Fantasma)". RacingKart.obj already has a material named
    /// "number_plate" (see the round-26 comment listing all 11 materials
    /// on KartVisualResourcePath) — a flat panel on the bodywork clearly
    /// meant for exactly this. Rather than importing external number
    /// decals (AGENTS.md rule 10: no unlicensed third-party assets), this
    /// draws the race number onto a small runtime Texture2D using a hand-
    /// coded 5x7 pixel digit font, the same "generate it, don't import it"
    /// approach ProceduralUITextures.cs already uses for the HUD. The
    /// generated texture is applied via CreateMaterial's existing texture
    /// overload (already used for the checkered flag) — no new material
    /// pipeline needed.
    /// </summary>
    internal static class KartNumberTexture
    {
        // Classic 5-column x 7-row pixel digits. Read top-to-bottom in this
        // array (row 0 = the glyph's TOP); DrawGlyph below flips this into
        // Unity's bottom-to-top SetPixel coordinate space.
        private static readonly string[][] DigitGlyphs =
        {
            new[] { " ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### " }, // 0
            new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " }, // 1
            new[] { " ### ", "#   #", "    #", "   # ", "  #  ", " #   ", "#####" }, // 2
            new[] { " ### ", "#   #", "    #", "  ## ", "    #", "#   #", " ### " }, // 3
            new[] { "   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # " }, // 4
            new[] { "#####", "#    ", "#### ", "    #", "    #", "#   #", " ### " }, // 5
            new[] { " ### ", "#    ", "#    ", "#### ", "#   #", "#   #", " ### " }, // 6
            new[] { "#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   " }, // 7
            new[] { " ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### " }, // 8
            new[] { " ### ", "#   #", "#   #", " ####", "    #", "    #", " ### " }, // 9
        };

        private const int GlyphCols = 5;
        private const int GlyphRows = 7;
        private const int GapCells = 1;

        /// <summary>
        /// Renders <paramref name="raceNumber"/> (clamped to 1..99 — this
        /// project's own race-number pool tops out at 20, see
        /// KartPhysicsPrototypeBootstrap.MaxRaceNumber, so 2 digits always
        /// fit) centered on a plain plate-colored square, dark digits on a
        /// pale background — matching how real kart racing number plates
        /// read (a legible high-contrast panel, not a colored decal).
        /// </summary>
        internal static Texture2D CreateRaceNumberTexture(int raceNumber, int size = 128)
        {
            size = Mathf.Max(32, size);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var background = new Color(0.95f, 0.95f, 0.95f, 1f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }
            texture.SetPixels(pixels);

            var digitsText = Mathf.Clamp(raceNumber, 1, 99).ToString();
            var digitColor = new Color(0.08f, 0.08f, 0.08f, 1f);

            var totalCellsWide = digitsText.Length * GlyphCols + Mathf.Max(0, digitsText.Length - 1) * GapCells;
            var cellSize = Mathf.Max(1, Mathf.FloorToInt(size * 0.65f / Mathf.Max(totalCellsWide, GlyphRows)));

            var blockWidth = totalCellsWide * cellSize;
            var blockHeight = GlyphRows * cellSize;
            var originX = (size - blockWidth) / 2;
            var originY = (size - blockHeight) / 2;

            var cursorX = originX;
            foreach (var digitChar in digitsText)
            {
                DrawGlyph(texture, DigitGlyphs[digitChar - '0'], cursorX, originY, cellSize, digitColor);
                cursorX += (GlyphCols + GapCells) * cellSize;
            }

            texture.Apply();
            return texture;
        }

        private static void DrawGlyph(Texture2D texture, string[] glyphRowsTopToBottom, int originX, int originY,
            int cellSize, Color color)
        {
            var textureWidth = texture.width;
            var textureHeight = texture.height;

            for (var row = 0; row < glyphRowsTopToBottom.Length; row++)
            {
                // Glyph row 0 is the TOP of the digit as written above, but
                // Unity's Texture2D.SetPixel treats row 0 as the BOTTOM of
                // the image, so the row index has to be flipped here.
                var textureRowIndex = glyphRowsTopToBottom.Length - 1 - row;
                var line = glyphRowsTopToBottom[row];

                for (var col = 0; col < line.Length; col++)
                {
                    if (line[col] != '#')
                    {
                        continue;
                    }

                    var pixelX0 = originX + col * cellSize;
                    var pixelY0 = originY + textureRowIndex * cellSize;

                    for (var dy = 0; dy < cellSize; dy++)
                    {
                        var pixelY = pixelY0 + dy;
                        if (pixelY < 0 || pixelY >= textureHeight)
                        {
                            continue;
                        }

                        for (var dx = 0; dx < cellSize; dx++)
                        {
                            var pixelX = pixelX0 + dx;
                            if (pixelX < 0 || pixelX >= textureWidth)
                            {
                                continue;
                            }

                            texture.SetPixel(pixelX, pixelY, color);
                        }
                    }
                }
            }
        }
    }
}
