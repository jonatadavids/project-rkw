using System;
using UnityEngine;
using UnityEngine.UI;

namespace RKW.UI
{
    public sealed class BootstrapStatusView : MonoBehaviour
    {
        private const string LoadingMessage = "Conectando...";
        private const string FailureMessage = "Não foi possível conectar. Tente novamente.";

        [SerializeField] private Text statusText;
        [SerializeField] private Button retryButton;

        private Action _retryAction;

        private void Awake()
        {
            retryButton.onClick.AddListener(HandleRetry);
        }

        private void OnDestroy()
        {
            retryButton.onClick.RemoveListener(HandleRetry);
        }

        public void ShowLoading()
        {
            statusText.text = LoadingMessage;
            statusText.gameObject.SetActive(true);
            retryButton.interactable = false;
            retryButton.gameObject.SetActive(false);
        }

        public void ShowFailure(Action retryAction)
        {
            _retryAction = retryAction;
            statusText.text = FailureMessage;
            statusText.gameObject.SetActive(true);
            retryButton.interactable = true;
            retryButton.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _retryAction = null;
            gameObject.SetActive(false);
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
