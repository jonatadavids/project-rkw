using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 45 (2026-09-01) founder feedback: "aquela tela de configuracao
    /// antes do jogo, pra salvar um padrao ao meu gosto" — he expected the
    /// CONFIGURAÇÕES item in the new main menu (see MainMenu) to hold the
    /// same choices as the pre-race setup screen (kart category, bot
    /// count/difficulty, number of laps — see SettingsMenu/RaceSetupMenu),
    /// but remembered as a personal default instead of being reset to the
    /// same hardcoded values every single race.
    ///
    /// PlayerPrefs-backed, same convention as <see cref="PlayerNameStore"/>
    /// (one value per device, no account/cloud sync). <see cref="HasSavedPreference"/>
    /// distinguishes "never saved anything yet" from "saved, and happened
    /// to match the defaults" so callers can tell whether to apply a saved
    /// preference at all.
    /// </summary>
    public static class RacePreferencesStore
    {
        private const string HasSavedKey = "RKW_Prefs_HasSaved";
        private const string UsesKartV2Key = "RKW_Prefs_UsesKartV2";
        private const string LapsKey = "RKW_Prefs_Laps";
        private const string SoloKey = "RKW_Prefs_Solo";
        private const string BotCountKey = "RKW_Prefs_BotCount";
        private const string DifficultyKey = "RKW_Prefs_Difficulty";
        // Rodada 46 (2026-09-01) founder feedback: "reiniciar a mesma
        // corrida... cai na tela de circuito e configuracao novamente ao
        // inves de ir direto" -- REINICIAR needs to know which track was
        // actually being raced so it can rebuild the SAME race directly
        // (see KartPhysicsPrototypeBootstrap.RequestQuickRestart). Saved
        // unconditionally every time a track is picked (TrackSelectMenu.
        // OnTrackSelected), unlike the kart/laps/bots/difficulty group
        // above which is only saved when the player explicitly visits
        // SettingsMenu -- there is no separate "did you ever pick a
        // track" concept to preserve here, a fresh install just defaults
        // to the oval like before.
        private const string UseTechnicalCircuit2Key = "RKW_Prefs_UseTechnicalCircuit2";

        // Rodada 46 (2026-09-01), second pass -- founder feedback: "ao
        // reiniciar ele volta a tela de configuracao" -- the round-46
        // quick-restart fix (see KartPhysicsPrototypeBootstrap.RequestQuickRestart)
        // skipped MainMenu and TrackSelectMenu, but missed that BeginRace()
        // ALSO always shows RaceSetupMenu (laps/bots/difficulty) as its
        // very last step -- that's the "tela de configuracao" the founder
        // saw again on restart. This is a SEPARATE record from the
        // UsesKartV2/Laps/Solo/BotCount/Difficulty group above: those are
        // only saved when the player explicitly visits SettingsMenu and
        // taps "SALVAR PADRÃO" (an intentional personal default), while
        // this one is saved automatically every time ANY race actually
        // starts (via OnRaceSetupConfirmed), specifically so a quick
        // restart can reproduce "whatever you just raced with" even if
        // you never opened SettingsMenu at all this session.
        private const string HasLastRaceSetupKey = "RKW_LastRace_HasSetup";
        private const string LastRaceLapsKey = "RKW_LastRace_Laps";
        private const string LastRaceBotCountKey = "RKW_LastRace_BotCount";
        private const string LastRaceDifficultyKey = "RKW_LastRace_Difficulty";

        public const int DefaultLaps = 3;
        public const int DefaultBotCount = 1;
        public const bool DefaultSolo = false;
        public const bool DefaultUsesKartV2 = false;
        public const BotDifficulty DefaultDifficulty = BotDifficulty.Medium;

        /// <summary>True once the player has saved a default at least once from SettingsMenu.</summary>
        public static bool HasSavedPreference => PlayerPrefs.GetInt(HasSavedKey, 0) == 1;

        public static bool PreferredUsesKartV2 => PlayerPrefs.GetInt(UsesKartV2Key, DefaultUsesKartV2 ? 1 : 0) == 1;

        public static int PreferredLaps => PlayerPrefs.GetInt(LapsKey, DefaultLaps);

        public static bool PreferredSolo => PlayerPrefs.GetInt(SoloKey, DefaultSolo ? 1 : 0) == 1;

        public static int PreferredBotCount => Mathf.Clamp(
            PlayerPrefs.GetInt(BotCountKey, DefaultBotCount), 1, RaceSetupMenu.MaxBotCount);

        public static BotDifficulty PreferredDifficulty
        {
            get
            {
                var raw = PlayerPrefs.GetInt(DifficultyKey, (int)DefaultDifficulty);
                return System.Enum.IsDefined(typeof(BotDifficulty), raw) ? (BotDifficulty)raw : DefaultDifficulty;
            }
        }

        /// <summary>Which track the player raced last (false = Circuito Oval, true = Circuito 2). Defaults to the oval on a fresh install.</summary>
        public static bool PreferredUseTechnicalCircuit2 => PlayerPrefs.GetInt(UseTechnicalCircuit2Key, 0) == 1;

        public static void Save(bool usesKartV2, int laps, bool solo, int botCount, BotDifficulty difficulty)
        {
            PlayerPrefs.SetInt(HasSavedKey, 1);
            PlayerPrefs.SetInt(UsesKartV2Key, usesKartV2 ? 1 : 0);
            PlayerPrefs.SetInt(LapsKey, laps);
            PlayerPrefs.SetInt(SoloKey, solo ? 1 : 0);
            PlayerPrefs.SetInt(BotCountKey, Mathf.Clamp(botCount, 1, RaceSetupMenu.MaxBotCount));
            PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
            PlayerPrefs.Save();
        }

        /// <summary>Remembers the track choice so a later quick-restart (see RequestQuickRestart) can rebuild the same race. Saved every time a track is picked, independent of the SettingsMenu "HasSaved" gate above.</summary>
        public static void SaveTrackChoice(bool useTechnicalCircuit2)
        {
            PlayerPrefs.SetInt(UseTechnicalCircuit2Key, useTechnicalCircuit2 ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Whether OnRaceSetupConfirmed has ever run this install -- guards against a quick-restart trying to reuse a race setup that never actually happened (should not be reachable in practice, but a fresh install/PlayMode test could hit it).</summary>
        public static bool HasLastRaceSetup => PlayerPrefs.GetInt(HasLastRaceSetupKey, 0) == 1;
        public static int LastRaceLaps => PlayerPrefs.GetInt(LastRaceLapsKey, DefaultLaps);
        public static int LastRaceBotCount => Mathf.Clamp(PlayerPrefs.GetInt(LastRaceBotCountKey, DefaultBotCount), 0, RaceSetupMenu.MaxBotCount);

        public static BotDifficulty LastRaceDifficulty
        {
            get
            {
                var raw = PlayerPrefs.GetInt(LastRaceDifficultyKey, (int)DefaultDifficulty);
                return System.Enum.IsDefined(typeof(BotDifficulty), raw) ? (BotDifficulty)raw : DefaultDifficulty;
            }
        }

        /// <summary>Remembers exactly what the player just raced with (laps/bots/difficulty) so RequestQuickRestart can rebuild the SAME race without showing RaceSetupMenu again. Called from OnRaceSetupConfirmed every time a race actually starts -- unlike Save() above, this is automatic, not an explicit "save as default" action.</summary>
        public static void SaveLastRaceSetup(int laps, int botCount, BotDifficulty difficulty)
        {
            PlayerPrefs.SetInt(HasLastRaceSetupKey, 1);
            PlayerPrefs.SetInt(LastRaceLapsKey, laps);
            PlayerPrefs.SetInt(LastRaceBotCountKey, Mathf.Clamp(botCount, 0, RaceSetupMenu.MaxBotCount));
            PlayerPrefs.SetInt(LastRaceDifficultyKey, (int)difficulty);
            PlayerPrefs.Save();
        }

        /// <summary>Resource path (see KartPhysicsPrototypeBootstrap's own constants) matching the saved kart preference.</summary>
        public static string PreferredKartModelResourcePath => PreferredUsesKartV2
            ? KartPhysicsPrototypeBootstrap.KartVisualV2ResourcePath
            : KartPhysicsPrototypeBootstrap.KartVisualResourcePath;

        public static string PreferredKartTuningResourcePath => PreferredUsesKartV2
            ? KartPhysicsPrototypeBootstrap.TuningV2ResourcePath
            : KartPhysicsPrototypeBootstrap.TuningResourcePath;
    }
}
