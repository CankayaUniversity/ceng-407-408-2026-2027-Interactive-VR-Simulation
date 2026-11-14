#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WorkplaceStudio.Desktop;

namespace WorkplaceStudio.Tests
{
    public sealed class InputGateTestWindow : EditorWindow { }

    public sealed class DesktopInputGateTests
    {
        InputGateTestWindow window;
        DesktopInputGate gate;
        VisualElement stage;
        Button button;
        bool captured;
        int clicks;

        [SetUp] public void SetUp()
        {
            window = ScriptableObject.CreateInstance<InputGateTestWindow>(); window.ShowUtility();
            stage = new VisualElement { focusable = true };
            button = new Button(() => clicks++) { text = "Save" };
            window.rootVisualElement.Add(stage); window.rootVisualElement.Add(button);
            captured = true; clicks = 0;
            gate = new DesktopInputGate(window.rootVisualElement, () => captured, stage);
            stage.Focus();
        }

        [TearDown] public void TearDown() { gate?.Dispose(); if (window != null) window.Close(); }

        [TestCase(KeyCode.W)] [TestCase(KeyCode.A)] [TestCase(KeyCode.S)] [TestCase(KeyCode.D)]
        public void MovementKeysDoNotReachUiControlsWhileCaptured(KeyCode key)
        {
            int received = 0; stage.RegisterCallback<KeyDownEvent>(_ => received++);
            using (var evt = KeyDownEvent.GetPooled('\0', key, EventModifiers.None))
            {
                stage.SendEvent(evt);
                Assert.That(evt.isImmediatePropagationStopped, Is.True);
            }
            Assert.That(received, Is.Zero);
            Assert.That(window.rootVisualElement.panel.focusController.focusedElement, Is.SameAs(stage));
        }

        [Test] public void UiNavigationCannotMoveFocusFromTheStageWhileCaptured()
        {
            using (var evt = NavigationMoveEvent.GetPooled(Vector2.down))
            {
                stage.SendEvent(evt);
                Assert.That(evt.isImmediatePropagationStopped, Is.True);
            }
            Assert.That(window.rootVisualElement.panel.focusController.focusedElement, Is.SameAs(stage));
        }

        [Test] public void UiSubmitCannotActivateAButtonWhileCaptured()
        {
            button.Focus();
            using (var evt = NavigationSubmitEvent.GetPooled()) button.SendEvent(evt);
            Assert.That(clicks, Is.Zero);
        }

        [Test] public void UiSubmitWorksAgainAfterMovementIsReleased()
        {
            captured = false; gate.SyncCapture(); button.Focus();
            using (var evt = NavigationSubmitEvent.GetPooled()) button.SendEvent(evt);
            Assert.That(clicks, Is.EqualTo(1));
        }

        [Test] public void CapturingClearsAnExistingButtonFocusWithoutDisablingTheButton()
        {
            captured = false; gate.SyncCapture(); button.Focus();
            Assert.That(window.rootVisualElement.panel.focusController.focusedElement, Is.SameAs(button));
            captured = true; gate.SyncCapture();
            Assert.That(button.focusable, Is.False);
            Assert.That(button.enabledSelf, Is.True);
            Assert.That(window.rootVisualElement.panel.focusController.focusedElement, Is.SameAs(stage));
            Assert.That(window.rootVisualElement.ClassListContains("input-captured"), Is.True);
        }

        [Test] public void ReleasingRestoresControlsWithoutLeavingASelectionRing()
        {
            captured = false; gate.SyncCapture();
            Assert.That(button.focusable, Is.True);
            Assert.That(window.rootVisualElement.panel.focusController.focusedElement, Is.Null);
            Assert.That(window.rootVisualElement.ClassListContains("input-captured"), Is.False);
        }
    }
}
#endif
