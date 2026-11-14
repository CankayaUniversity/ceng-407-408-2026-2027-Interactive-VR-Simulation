using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace WorkplaceStudio.Desktop
{
    // Movement reads the Input System directly. UI Toolkit must not turn the
    // same WASD/Tab/Enter or pointer input into UI focus changes and clicks.
    public sealed class DesktopInputGate : IDisposable
    {
        readonly VisualElement root;
        readonly Func<bool> captured;
        readonly VisualElement viewport;
        readonly List<VisualElement> focusableControls = new List<VisualElement>();
        bool wasCaptured;

        public DesktopInputGate(VisualElement root, Func<bool> captured, VisualElement viewport = null)
        {
            this.root = root; this.captured = captured; this.viewport = viewport;
            root.RegisterCallback<NavigationMoveEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationCancelEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<ClickEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.RegisterCallback<WheelEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            SyncCapture();
        }

        public void SyncCapture()
        {
            bool active = captured();
            if (active != wasCaptured)
            {
                wasCaptured = active;
                root.EnableInClassList("input-captured", active);
                if (active)
                {
                    root.focusController?.focusedElement?.Blur();
                    root.Query<VisualElement>().ForEach(element =>
                    {
                        if (element != viewport && element.focusable)
                        { focusableControls.Add(element); element.focusable = false; }
                    });
                }
                else RestoreFocusability();
            }
            // Runtime UI navigation can arrive through a panel-level focus ring.
            // Keep its only focus target inside the viewport while moving.
            if (active && viewport != null && root.focusController?.focusedElement != viewport)
            { root.focusController?.focusedElement?.Blur(); viewport.Focus(); }
        }

        void RestoreFocusability()
        {
            foreach (var control in focusableControls) control.focusable = true;
            focusableControls.Clear(); viewport?.Blur();
        }

        void BlockWhileCaptured(EventBase evt)
        {
            // Trickle-down stops controls' default focus/navigation actions as
            // well as their callbacks. Esc is handled by DesktopNavigation,
            // independently of UI focus; window focus loss still releases it.
            if (captured()) evt.StopImmediatePropagation();
        }

        public void Dispose()
        {
            RestoreFocusability(); root.RemoveFromClassList("input-captured");
            root.UnregisterCallback<NavigationMoveEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<NavigationSubmitEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<NavigationCancelEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyDownEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyUpEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerDownEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<ClickEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
            root.UnregisterCallback<WheelEvent>(BlockWhileCaptured, TrickleDown.TrickleDown);
        }
    }
}
