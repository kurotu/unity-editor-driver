using System;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Playwright-like locator for interacting with IMGUI controls.
    /// Finds controls by label in windows that use GUITracker.
    /// </summary>
    public class Locator
    {
        private readonly EditorWindow _window;
        private readonly string _label;

        internal Locator(EditorWindow window, string label)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public string Label => _label;

        /// <summary>
        /// Returns the screen rect of the control found by label via GUITracker.
        /// </summary>
        public Rect GetRect()
        {
            var entry = ResolveEntry();
            return entry.Rect;
        }

        /// <summary>
        /// Clicks at the center of the control.
        /// </summary>
        public void Click()
        {
            var rect = GetRect();
            InputSimulator.MouseClick(_window, rect.center);
        }

        /// <summary>
        /// Clicks the control and types text into it (for text fields).
        /// </summary>
        public void Fill(string text)
        {
            Click();
            // Select all existing text — use Command on macOS, Control elsewhere
            var selectAllModifier = Application.platform == RuntimePlatform.OSXEditor
                ? EventModifiers.Command
                : EventModifiers.Control;
            InputSimulator.KeyDown(_window, KeyCode.A, selectAllModifier);
            InputSimulator.KeyUp(_window, KeyCode.A, selectAllModifier);
            InputSimulator.TypeText(_window, text);
        }

        /// <summary>
        /// Clicks the control to toggle it (for checkboxes).
        /// </summary>
        public void Toggle()
        {
            Click();
        }

        /// <summary>
        /// Sets a slider to a normalized position (0..1) by clicking at the interpolated x.
        /// </summary>
        public void SetSlider(float normalizedValue)
        {
            var rect = GetRect();
            // EditorGUI sliders have the label on the left (labelWidth) and the slider on the right
            float labelWidth = EditorGUIUtility.labelWidth;
            float sliderLeft = rect.x + labelWidth;
            float sliderWidth = rect.width - labelWidth;
            float x = sliderLeft + sliderWidth * Mathf.Clamp01(normalizedValue);
            float y = rect.center.y;
            InputSimulator.MouseClick(_window, new Vector2(x, y));
        }

        private ControlEntry ResolveEntry()
        {
            var tracker = GUITracker.GetTracker(_window);
            if (tracker == null)
            {
                throw new InvalidOperationException(
                    $"No GUITracker found for window '{_window.GetType().Name}'. " +
                    "Ensure the window calls GUITracker.Begin(this) in OnGUI.");
            }

            var entry = tracker.FindByLabel(_label);
            if (!entry.HasValue)
            {
                throw new InvalidOperationException(
                    $"Control with label '{_label}' not found. " +
                    $"Available labels: [{string.Join(", ", GetAvailableLabels(tracker))}]");
            }

            return entry.Value;
        }

        private static string[] GetAvailableLabels(GUITracker tracker)
        {
            var entries = tracker.Entries;
            var labels = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                labels[i] = entries[i].Label;
            }
            return labels;
        }
    }
}
