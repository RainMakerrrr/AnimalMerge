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
    public class AddAnimalButtonViewTests
    {
        private static readonly Color DisabledColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        // ─── Helpers ────────────────────────────────────────────────────

        private (AddAnimalButtonView view, Button button, Image graphic) CreateView()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.AddComponent<Canvas>();

            var buttonGo = new GameObject("AddAnimalButton");
            buttonGo.transform.SetParent(canvasGo.transform);
            buttonGo.AddComponent<RectTransform>();

            var graphic = buttonGo.AddComponent<Image>();
            var button = buttonGo.AddComponent<Button>();
            var view = buttonGo.AddComponent<AddAnimalButtonView>();

            typeof(AddAnimalButtonView)
                .GetField("_targetGraphic", BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(view, graphic);
            typeof(AddAnimalButtonView)
                .GetField("_disabledColor", BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(view, DisabledColor);

            return (view, button, graphic);
        }

        // ─── Tests ─────────────────────────────────────────────────────

        /// <summary>IT-ADDBTN-001: A click is reported as a Clicked event, the view runs no logic itself</summary>
        [UnityTest]
        public IEnumerator Click_RaisesClickedEvent()
        {
            // Arrange
            var (view, button, _) = CreateView();
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

        /// <summary>IT-ADDBTN-002: A greyed out button is not interactable and is tinted</summary>
        [UnityTest]
        public IEnumerator SetInteractable_False_DisablesAndTintsButton()
        {
            // Arrange
            var (view, button, graphic) = CreateView();
            yield return null;

            // Act
            view.SetInteractable(false);

            // Assert
            button.interactable.Should().BeFalse();
            graphic.color.Should().Be(DisabledColor);

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }

        /// <summary>IT-ADDBTN-003: SetVisible toggles the button GameObject</summary>
        [UnityTest]
        public IEnumerator SetVisible_TogglesGameObject()
        {
            // Arrange
            var (view, _, _) = CreateView();
            yield return null;

            // Act
            view.SetVisible(false);

            // Assert
            view.gameObject.activeSelf.Should().BeFalse();

            // Act
            view.SetVisible(true);

            // Assert
            view.gameObject.activeSelf.Should().BeTrue();

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
        }
    }
}
