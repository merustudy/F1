using TMPro;
using UnityEngine;

namespace F1.Core
{
    /// <summary>
    /// Boot screen. It must work without Addressables and Localization, so it uses fixed English text
    /// and a directly referenced Latin font.
    /// </summary>
    public sealed class BootstrapView : MonoBehaviour
    {
        [SerializeField] TMP_Text _status;
        [SerializeField] TMP_Text _error;

        public string StatusText => _status.text;
        public string ErrorText => _error.text;

        public void ShowLoading()
        {
            gameObject.SetActive(true);
            _status.text = "Loading...";
            _error.text = string.Empty;
        }

        public void ShowError(BootStep step)
        {
            gameObject.SetActive(true);
            _status.text = "Startup failed";
            _error.text = $"{step.Name} ({step.ErrorCode})";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
