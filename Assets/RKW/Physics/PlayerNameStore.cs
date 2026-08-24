using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder playtest feedback, 2026-08-20 (round 8): "nao conseguir...
    /// nem pra digitar o meu [nome] pra ficar gravado a melhor volta
    /// nominalmente". A single PlayerPrefs-backed name for this device —
    /// used to tag recorded best laps (<see cref="LapRecordStore"/>) and to
    /// label the player's own row in the live standings HUD instead of a
    /// generic "VOCÊ".
    /// </summary>
    public static class PlayerNameStore
    {
        private const string NameKey = "RKW_PlayerName";
        // Founder playtest feedback, 2026-08-20 (round 9): "quando coloquei
        // o nome do Piloto a primeira vez parecia que tinha a palavra Pilot
        // na frente... ideal ficar sem conteúdo nenhum... caso ele nao
        // digite nada, usar user1". "Piloto" was also being passed as the
        // pre-filled text when opening the on-screen keyboard (see
        // RaceSetupMenu), which read as if it had already typed something.
        // The keyboard now always opens blank; this is only the fallback
        // shown in the HUD/leaderboard if the player never typed anything.
        private const string DefaultName = "User1";
        private const int MaxNameLength = 18;

        public static string GetName()
        {
            var stored = PlayerPrefs.GetString(NameKey, string.Empty);
            return string.IsNullOrWhiteSpace(stored) ? DefaultName : stored;
        }

        public static void SetName(string name)
        {
            var sanitized = Sanitize(name);
            if (string.IsNullOrEmpty(sanitized))
            {
                return;
            }

            PlayerPrefs.SetString(NameKey, sanitized);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Strips the characters LapRecordStore's encoding relies on as
        /// separators (so a typed name can never corrupt the saved lap
        /// history) and clamps length for the on-screen HUD/standings.
        /// </summary>
        private static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var cleaned = name.Trim().Replace(";", string.Empty).Replace(",", string.Empty);
            return cleaned.Length > MaxNameLength ? cleaned.Substring(0, MaxNameLength) : cleaned;
        }
    }
}
