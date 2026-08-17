using System;
using UnityEngine;
using UnityEngine.UI;

namespace RKW.UI
{
    public sealed class BootstrapStatusView : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private Button retryButton;

        private Action _retryAction;
        private Text _retryText;
        private State _state;

        private enum State
        {
            Hidden,
            Loading,
            Failure
        }

        private void Awake()
        {
            _retryText = retryButton.GetComponentInChildren<Text>(true);
            retryButton.onClick.AddListener(HandleRetry);
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged +=
                HandleLocaleChanged;
            RefreshLocalizedText();
        }

        private void OnDestroy()
        {
            retryButton.onClick.RemoveListener(HandleRetry);
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -=
                HandleLocaleChanged;
        }

        public void ShowLoading()
        {
            _state = State.Loading;
            RefreshLocalizedText();
            statusText.gameObject.SetActive(true);
            retryButton.interactable = false;
            retryButton.gameObject.SetActive(false);
        }

        public void ShowFailure(Action retryAction)
        {
            _retryAction = retryAction;
            _state = State.Failure;
            RefreshLocalizedText();
            statusText.gameObject.SetActive(true);
            retryButton.interactable = true;
            retryButton.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _retryAction = null;
            _state = State.Hidden;
            gameObject.SetActive(false);
        }

        private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            RefreshLocalizedText();
        }

        private void RefreshLocalizedText()
        {
            if (_retryText != null)
            {
                _retryText.text = UiLocalization.Get(UiLocalization.BootstrapRetry);
            }

            if (_state == State.Loading)
            {
                statusText.text = UiLocalization.Get(UiLocalization.BootstrapConnecting);
            }
            else if (_state == State.Failure)
            {
                statusText.text = UiLocalization.Get(UiLocalization.BootstrapConnectionFailed);
            }
            else
            {
                statusText.text = UiLocalization.EmergencyMessage;
            }
        }

        private void HandleRetry()
        {
            var retryAction = _retryAction;
            if (retryAction == null)
            {
                return;
            }

            _retryAction = null;
            retryButton.interactable = false;
            retryButton.gameObject.SetActive(false);
            retryAction.Invoke();
        }
    }
}
