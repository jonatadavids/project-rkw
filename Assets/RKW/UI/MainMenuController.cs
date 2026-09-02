using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RKW.UI
{
    /// <summary>
    /// Rodada 46 (2026-09-01) founder decision, first pass: this Canvas
    /// menu (reached after a real, anonymous Unity Gaming Services
    /// sign-in -- see BootstrapController) used to treat JOGAR exactly
    /// like ESCOLA/GARAGEM -- a "coming soon" placeholder, never actually
    /// wired to the race. JOGAR now loads the real race scene.
    ///
    /// Rodada 46, terceira passada, mesmo dia -- founder feedback: "a tela
    /// de menu foi uma bem antiga... queria aquela tela mesmo com aquelas
    /// cores". This Canvas's own visual design (plain default
    /// Unity UI buttons/Text, no styling at all) was never meant to be
    /// the final look -- it was built to test the login/navigation
    /// ARCHITECTURE, not the approved visual design. Rather than
    /// hand-edit this scene's serialized Button/Text hierarchy blind (no
    /// Editor available to verify it), this Canvas's rendering is now
    /// switched off entirely (`canvas.enabled = false` -- its
    /// GameObjects/components stay exactly as they were, so the existing
    /// PlayMode tests that find them by name and invoke onClick directly
    /// keep working unchanged) and RKW.Physics.MainMenu -- a faithful
    /// OnGUI reproduction of the actual approved mockup (see that class's
    /// own doc comment for the file it was built from and what had to be
    /// substituted because the underlying feature doesn't exist yet) --
    /// is drawn on top instead, as the thing a real player actually sees
    /// and taps.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button schoolButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Text feedbackText;

        private void Awake()
        {
            playButton.onClick.AddListener(StartRace);
            schoolButton.onClick.AddListener(ShowComingSoon);
            garageButton.onClick.AddListener(ShowComingSoon);
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged +=
                HandleLocaleChanged;
            RefreshLocalizedText();
            feedbackText.gameObject.SetActive(false);

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            ShowStyledMainMenu();
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(StartRace);
            schoolButton.onClick.RemoveListener(ShowComingSoon);
            garageButton.onClick.RemoveListener(ShowComingSoon);
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -=
                HandleLocaleChanged;
        }

        /// <summary>
        /// Rodada 46: loads the race scene by itself (LoadSceneMode.Single),
        /// which unloads Bootstrap+MainMenu in the process -- KartPhysicsPrototypeBootstrap.Awake()
        /// expects to be the only thing running (see its own round-46
        /// comment: it now skips straight to TrackSelectMenu instead of
        /// showing a second main menu on top of this one).
        /// </summary>
        private static void StartRace()
        {
            SceneManager.LoadScene("KartPhysicsPrototype", LoadSceneMode.Single);
        }

        private void ShowComingSoon()
        {
            feedbackText.text = UiLocalization.Get(UiLocalization.MenuComingSoon);
            feedbackText.gameObject.SetActive(true);
        }

        // Round 46, terceira passada: the styled RKW.Physics.MainMenu now
        // has its own CONFIGURAÇÕES row (real button, opens SettingsMenu),
        // so the small standalone OnGUI settings button this method used
        // to draw is gone -- it would just be a second, redundant entry
        // point to the exact same screen now.
        private static void ShowStyledMainMenu()
        {
            var mainMenuObject = new GameObject("MainMenu");
            // Unqualified MainMenu resolves to RKW.Physics.MainMenu via the
            // `using RKW.Physics;` above -- NOT UnityEngine.Physics (a
            // static class also in scope via `using UnityEngine;`, but
            // that one has no nested type named MainMenu, so there's no
            // real ambiguity here despite the similar-looking names).
            var mainMenu = mainMenuObject.AddComponent<MainMenu>();
            mainMenu.Configure(StartRace, ShowSettingsFromStyledMainMenu);
        }

        private static void ShowSettingsFromStyledMainMenu()
        {
            var settingsObject = new GameObject("SettingsMenu");
            var settingsMenu = settingsObject.AddComponent<SettingsMenu>();
            // Same back-and-forth pattern KartPhysicsPrototypeBootstrap
            // already uses for its own MainMenu/SettingsMenu round trip --
            // closing SettingsMenu (Salvar or Voltar) re-shows this menu.
            settingsMenu.Configure(ShowStyledMainMenu);
        }

        private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            RefreshLocalizedText();
        }

        private void RefreshLocalizedText()
        {
            playButton.GetComponentInChildren<Text>(true).text =
                UiLocalization.Get(UiLocalization.MenuPlay);
            schoolButton.GetComponentInChildren<Text>(true).text =
                UiLocalization.Get(UiLocalization.MenuSchool);
            garageButton.GetComponentInChildren<Text>(true).text =
                UiLocalization.Get(UiLocalization.MenuGarage);
            feedbackText.text = UiLocalization.Get(UiLocalization.MenuComingSoon);
        }
    }
}
