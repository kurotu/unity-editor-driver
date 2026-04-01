using System;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Playwright-like locator for interacting with IMGUI controls.
    /// Uses pre-computed rects from <see cref="ImguiPage"/> — no GUITracker required.
    /// </summary>
    public class ImguiLocator
    {
        private readonly EditorWindow _window;
        private readonly ImguiControlInfo _control;

        internal ImguiLocator(EditorWindow window, ImguiControlInfo control)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _control = control ?? throw new ArgumentNullException(nameof(control));
        }

        /// <summary>The label text of this control.</summary>
        public string Label => _control.Label;

        /// <summary>The computed rect of this control in window-local coordinates.</summary>
        public Rect Rect => _control.Rect;

        /// <summary>The type of IMGUI control.</summary>
        public ControlType ControlType => _control.Type;

        /// <summary>
        /// Clicks the center of this control.
        /// </summary>
        public void Click()
        {
            InputSimulator.MouseClick(_window, _control.Rect.center);
        }

        /// <summary>
        /// Clicks the control's input area, selects all, then types the given text.
        /// Suitable for TextField and similar input controls.
        /// </summary>
        public void Fill(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var rect = _control.Rect;
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldX = rect.x + labelWidth + (rect.width - labelWidth) / 2f;
            var fieldCenter = new Vector2(fieldX, rect.center.y);

            InputSimulator.MouseClick(_window, fieldCenter);

            // Select all existing text
            var selectAllMod = Application.platform == RuntimePlatform.OSXEditor
                ? EventModifiers.Command
                : EventModifiers.Control;
            InputSimulator.KeyDown(_window, KeyCode.A, selectAllMod);
            InputSimulator.KeyUp(_window, KeyCode.A, selectAllMod);

            InputSimulator.TypeText(_window, text);
        }

        /// <summary>
        /// Clicks the control to toggle its state.
        /// </summary>
        public void Toggle()
        {
            Click();
        }

        /// <summary>
        /// Sets a slider to a value by clicking at the corresponding position.
        /// <paramref name="normalizedValue"/> is 0..1 mapped to the slider's visual range.
        /// </summary>
        public void SetSlider(float normalizedValue)
        {
            var rect = _control.Rect;
            float labelWidth = EditorGUIUtility.labelWidth;
            float sliderLeft = rect.x + labelWidth;
            float sliderWidth = rect.width - labelWidth;
            float x = sliderLeft + sliderWidth * Mathf.Clamp01(normalizedValue);
            InputSimulator.MouseClick(_window, new Vector2(x, rect.center.y));
        }
    }
}
