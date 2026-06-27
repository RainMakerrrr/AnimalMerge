using System.Collections;
using System.Reflection;
using Code.Animals.UI;
using FluentAssertions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Code.Tests.PlayModeTests.UI
{
    public class DamagePopupViewTests
    {
        // ─── Helpers ───────────────────────────────────────────────────────────

        private (DamagePopupView view, TextMeshProUGUI text) CreatePopupView(float duration = 1.2f)
        {
            // Canvas parent — required for CanvasRenderer/TextMeshProUGUI to function
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Popup root
            var popupGo = new GameObject("DamagePopup");
            popupGo.transform.SetParent(canvasGo.transform);
            popupGo.AddComponent<RectTransform>();

            // Text child
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(popupGo.transform);
            textGo.AddComponent<RectTransform>();
            var text = textGo.AddComponent<TextMeshProUGUI>();

            // DamagePopupView — Awake grabs RectTransform
            var view = popupGo.AddComponent<DamagePopupView>();

            // Wire text field and override duration for fast tests
            typeof(DamagePopupView)
                .GetField("_text", BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(view, text);
            typeof(DamagePopupView)
                .GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(view, duration);

            return (view, text);
        }

        // ─── Tests ─────────────────────────────────────────────────────────────

        /// <summary>IT-DPV-001: Play() sets the displayed text content immediately</summary>
        [UnityTest]
        public IEnumerator Play_SetsTextContent()
        {
            // Arrange
            var (view, text) = CreatePopupView();

            // Act
            view.Play("-25", Color.red);
            yield return null;

            // Assert
            text.text.Should().Be("-25");

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }

        /// <summary>IT-DPV-002: Play() applies the specified color to the text</summary>
        [UnityTest]
        public IEnumerator Play_SetsTextColor()
        {
            // Arrange
            var (view, text) = CreatePopupView();

            // Act
            view.Play("Miss!", Color.yellow);
            yield return null;

            // Assert
            text.color.Should().Be(Color.yellow);

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }

        /// <summary>IT-DPV-003: Play() destroys the GameObject after the animation duration</summary>
        [UnityTest]
        public IEnumerator Play_DestroysGameObjectAfterDuration()
        {
            // Arrange — short duration so test runs fast
            var (view, _) = CreatePopupView(duration: 0.1f);
            var canvasRoot = view.transform.root.gameObject;

            // Act
            view.Play("-10", Color.red);
            yield return new WaitForSeconds(0.4f); // 4× duration buffer

            // Assert — view destroyed (Unity fake-null after Destroy)
            (view == null).Should().BeTrue("popup should self-destroy after animation completes");

            // Cleanup — canvas root may still exist
            if (canvasRoot != null)
                Object.DestroyImmediate(canvasRoot);
        }
    }
}
