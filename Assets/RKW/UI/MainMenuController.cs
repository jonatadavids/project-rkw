using UnityEngine;
using UnityEngine.UI;

namespace RKW.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string ComingSoonText = "Disponível em breve";

        [SerializeField] private Button playButton;
        [SerializeField] private Button schoolButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Text feedbackText;

        private void Awake()
        {
            playButton.onClick.AddListener(ShowComingSoon);
            schoolButton.onClick.AddListener(ShowComingSoon);
            garageButton.onClick.AddListener(ShowComingSoon);
            feedbackText.text = ComingSoonText;
            feedbackText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(ShowComingSoon);
            schoolButton.onClick.RemoveListener(ShowComingSoon);
            garageButton.onClick.RemoveListener(ShowComingSoon);
        }

        private void ShowComingSoon()
        {
            feedbackText.text = ComingSoonText;
            feedbackText.gameObject.SetActive(true);
        }
    }
}
