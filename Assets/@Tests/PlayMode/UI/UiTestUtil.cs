using System.Collections;
using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace F1.Tests
{
    /// <summary>Drives the screens the way a player does: by clicking buttons and reading texts.</summary>
    internal static class UiTestUtil
    {
        public const float DefaultTimeoutSeconds = 20f;

        /// <summary>Boots the app in a locale and waits for the title screen.</summary>
        public static IEnumerator BootToTitle(string saveRoot, string localeCode)
        {
            BootTestUtil.WriteSettings(saveRoot, localeCode);
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();
            Assert.AreEqual(InitializationState.Initialized, AppRoot.Current.State);
            Assert.AreEqual(ScreenId.Title, Managers.UI.CurrentId);
        }

        public static IEnumerator WaitForScreen(ScreenId id, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Managers.UI.CurrentId != id || Managers.UI.IsBusy)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"Screen {id} was not shown; the screen is {Managers.UI.CurrentId}.");
                }

                yield return null;
            }

            // One more frame so layout groups and localized labels have settled.
            yield return null;
        }

        /// <summary>
        /// Waits until what a screen just showed or hid can be clicked. A panel that was switched on
        /// takes part in pointer raycasts only after the canvas has drawn it once.
        /// </summary>
        public static IEnumerator WaitForRedraw()
        {
            yield return null;
            yield return null;
        }

        public static T Screen<T>()
            where T : UIScreen
        {
            Assert.IsInstanceOf<T>(Managers.UI.Current);
            return (T)Managers.UI.Current;
        }

        public static Transform At(Component root, string path)
        {
            Transform found = root.transform.Find(path);
            Assert.IsNotNull(found, $"'{path}' was not found under {root.name}.");
            return found;
        }

        public static string TextAt(Component root, string path)
        {
            return At(root, path).GetComponent<TMP_Text>().text;
        }

        public static Button ButtonAt(Component root, string path)
        {
            var button = At(root, path).GetComponent<Button>();
            Assert.IsNotNull(button, $"'{path}' is not a button.");
            return button;
        }

        /// <summary>Clicks a button. Fails when a player could not click it: hidden, disabled or covered by something else.</summary>
        public static void Click(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"Button '{button.name}' is not shown.");
            Assert.IsTrue(button.interactable, $"Button '{button.name}' is disabled.");
            AssertPointerReaches(button);
            button.onClick.Invoke();
        }

        static void AssertPointerReaches(Button button)
        {
            Transform top = TopmostUnderPointer(button);
            Assert.IsNotNull(top, $"A click in the middle of button '{button.name}' hits nothing.");
            Assert.IsTrue(top == button.transform || top.IsChildOf(button.transform), $"'{top.name}' covers button '{button.name}'.");
        }

        /// <summary>True when a mouse click in the middle of the button would land on it and not on something above it.</summary>
        public static bool PointerReaches(Button button)
        {
            Transform top = TopmostUnderPointer(button);
            return top != null && (top == button.transform || top.IsChildOf(button.transform));
        }

        /// <summary>Casts a pointer ray at the middle of the button, the way the event system does for a mouse click.</summary>
        static Transform TopmostUnderPointer(Button button)
        {
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            var pointer = new PointerEventData(EventSystem.current) { position = (corners[0] + corners[2]) * 0.5f };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits.Count == 0 ? null : hits[0].gameObject.transform;
        }

        public static void Click(Component root, string path)
        {
            Click(ButtonAt(root, path));
        }

        /// <summary>The shown clones of a template, in display order.</summary>
        public static T[] Views<T>(Component root)
            where T : Component
        {
            return root.GetComponentsInChildren<T>(false);
        }

        public static T ViewNamed<T>(Component root, string text)
            where T : Component
        {
            T view = Views<T>(root).FirstOrDefault(v => v.GetComponentsInChildren<TMP_Text>().Any(t => t.text == text));
            Assert.IsNotNull(view, $"No {typeof(T).Name} shows '{text}'.");
            return view;
        }
    }
}
