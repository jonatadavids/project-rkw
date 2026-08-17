using UnityEngine;
using UnityEngine.UI;

namespace RKW.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button schoolButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Text feedbackText;

        private void Awake()
        {
            playButton.onClick.AddListener(ShowComingSoon);
            schoolButton.onClick.AddListener(ShowComingSoon);
            garageButton.onClick.AddListener(ShowComingSoon);
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged +=
                HandleLocaleChanged;
            RefreshLocalizedText();
            feedbackText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(ShowComingSoon);
            schoolButton.onClick.RemoveListener(ShowComingSoon);
            garageButton.onClick.RemoveListener(ShowComingSoon);
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -=
                HandleLocaleChanged;
        }

        private void ShowComingSoon()
        {
            feedbackText.text = UiLocalization.Get(UiLocalization.MenuComingSoon);
            feedbackText.gameObject.SetActive(true);
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
