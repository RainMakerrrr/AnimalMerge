using System.Collections;
using System.Reflection;
using Code.Battle.UI;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Code.Tests.PlayModeTests.UI
{
    public class BattleButtonViewTests
    {
        // ─── Helpers ───────────────────────────────────────────────────

        private (BattleButtonView view, Button button, UiPulseAnimator pulse) CreateView()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.AddComponent<Canvas>();

            var buttonGo = new GameObject("BattleButton");
            buttonGo.transform.SetParent(canvasGo.transform);
            buttonGo.AddComponent<RectTransform>();

            var graphic = buttonGo.AddComponent<Image>();
            var button = buttonGo.AddComponent<Button>();
            var pulse = buttonGo.AddComponent<UiPulseAnimator>();
            var view = buttonGo.AddComponent<BattleButtonView>();

            SetPrivateField(view, "_targetGraphic", graphic);
            SetPrivateField(view, "_pulse", pulse);

            return (view, button, pulse);
        }

        private static void SetPrivateField(object target, string fieldName, object value) =>
            target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(target, value);

        // ─── Tests ─────────────────────────────────────────────────────

        /// <summary>IT-BTLBTN-001: A ready button is interactable and pulses</summary>
        [UnityTest]
        public IEnumerator SetReady_True_EnablesButtonAndPulses()
        {
            // Arrange
            var (view, button, pulse) = CreateView();
            view.SetVisible(true);
            yield return null;

            // Act
            view.SetReady(true);
            yield return null;

            // Assert
            button.interactable.Should().BeTrue();
            pulse.IsPlaying.Should().BeTrue();

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }

        /// <summary>IT-BTLBTN-002: Losing readiness stops the pulse and restores the original scale</summary>
        [UnityTest]
        public IEnumerator SetReady_False_StopsPulseAndRestoresScale()
        {
            // Arrange
            var (view, button, pulse) = CreateView();
            view.SetVisible(true);
            view.SetReady(true);
            yield return null;
            yield return null;

            // Act
            view.SetReady(false);

            // Assert
            button.interactable.Should().BeFalse();
            pulse.IsPlaying.Should().BeFalse();
            view.transform.localScale.Should().Be(Vector3.one);

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }

        /// <summary>IT-BTLBTN-003: A click is reported as a Clicked event</summary>
        [UnityTest]
        public IEnumerator Click_RaisesClickedEvent()
        {
            // Arrange
            var (view, button, _) = CreateView();
            view.SetVisible(true);
            view.SetReady(true);
            int clicks = 0;
            view.Clicked += () => clicks++;
            yield return null;

            // Act
            button.onClick.Invoke();

            // Assert
            clicks.Should().Be(1);

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }

        /// <summary>IT-BTLBTN-004: Hiding the button also stops the pulse</summary>
        [UnityTest]
        public IEnumerator SetVisible_False_HidesButtonAndStopsPulse()
        {
            // Arrange
            var (view, _, pulse) = CreateView();
            view.SetVisible(true);
            view.SetReady(true);
            yield return null;

            // Act
            view.SetVisible(false);

            // Assert
            view.gameObject.activeSelf.Should().BeFalse();
            pulse.IsPlaying.Should().BeFalse();

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }
    }
}
