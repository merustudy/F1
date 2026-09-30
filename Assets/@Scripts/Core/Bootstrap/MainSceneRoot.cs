using UnityEngine;
using UnityEngine.SceneManagement;

namespace F1.Core
{
    /// <summary>Entry object of the Main scene. AppRoot binds it after the scene is loaded.</summary>
    public sealed class MainSceneRoot : MonoBehaviour
    {
        [SerializeField] RectTransform _uiRoot;

        public RectTransform UiRoot => _uiRoot;
        public bool IsBound { get; private set; }

        void Awake()
        {
#if UNITY_EDITOR
            // Playing the Main scene directly in the Editor goes through Boot first.
            if (AppRoot.Current == null)
            {
                SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            }
#endif
        }

        public void Bind()
        {
            IsBound = true;
        }
    }
}
