using System;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Playwright-like locator for interacting with EditorWindow controls.
    /// Technology-agnostic: works for both IMGUI and UIElements windows.
    /// All interactions are black-box: no reflection into the target window.
    /// </summary>
    public class Locator
    {
        private readonly EditorWindow _window;
        private readonly ControlInfo _control;

        internal Locator(EditorWindow window, ControlInfo control)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _control = control ?? throw new ArgumentNullException(nameof(control));
        }

        /// <summary>The label text of this control.</summary>
        public string Label => _control.Label;

        /// <summary>The computed rect of this control in window-local coordinates.</summary>
        public Rect Rect => _control.Rect;

        /// <summary>The type of control.</summary>
        public ControlType ControlType => _control.Type;

        /// <summary>
        /// Clicks the center of this control.
        /// </summary>
        public void Click()
        {
            InputSimulator.MouseClick(_window, _control.Rect.center);
        }

        /// <summary>
        /// Reads the displayed text from the control via the clipboard.
        /// Clicks the input area, selects all text, copies to clipboard, then returns it.
        /// Supported for <see cref="UniEditWright.ControlType.TextField"/>,
        /// <see cref="UniEditWright.ControlType.IntField"/>,
        /// <see cref="UniEditWright.ControlType.FloatField"/>, and
        /// <see cref="UniEditWright.ControlType.Slider"/> (reads the numeric field).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called on a control type that does not have a readable text field.
        /// </exception>
        public string ReadText()
        {
            EnsureReadableType();

            var clickPos = GetFieldClickPosition();
            string saved = GUIUtility.systemCopyBuffer;
            GUIUtility.systemCopyBuffer = "";

            try
            {
                // Focus the text input area
                InputSimulator.MouseClick(_window, clickPos);

                // Select all + copy via IMGUI command events
                InputSimulator.SendCommand(_window, "SelectAll");
                InputSimulator.SendCommand(_window, "Copy");

                return GUIUtility.systemCopyBuffer;
            }
            finally
            {
                GUIUtility.systemCopyBuffer = saved;
            }
        }

        /// <summary>
        /// Clicks the control's input area, selects all, then types the given text.
        /// Suitable for TextField and similar input controls.
        /// </summary>
        public void Fill(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var clickPos = GetFieldClickPosition();
            InputSimulator.MouseClick(_window, clickPos);

            // Select all via IMGUI command event, then type replacement text
            InputSimulator.SendCommand(_window, "SelectAll");

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
            float fieldWidth = EditorGUIUtility.fieldWidth;
            float sliderLeft = rect.x + labelWidth;
            float sliderWidth = rect.width - labelWidth - fieldWidth;
            float x = sliderLeft + sliderWidth * Mathf.Clamp01(normalizedValue);
            InputSimulator.MouseClick(_window, new Vector2(x, rect.center.y));
        }

        /// <summary>
        /// Captures a screenshot of the region occupied by this control.
        /// Returns a <see cref="Texture2D"/> that the caller must destroy.
        /// </summary>
        public Texture2D CaptureScreenshot()
        {
            var windowTex = ScreenshotCapture.CaptureWindow(_window);
            try
            {
                var rect = _control.Rect;
                int x = Mathf.RoundToInt(rect.x);
                int y = Mathf.RoundToInt(rect.y);
                int w = Mathf.RoundToInt(rect.width);
                int h = Mathf.RoundToInt(rect.height);

                // Window texture is bottom-up; flip y
                int flippedY = windowTex.height - y - h;

                var pixels = windowTex.GetPixels(x, flippedY, w, h);
                var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
                cropped.SetPixels(pixels);
                cropped.Apply();
                return cropped;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(windowTex);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────

        private Vector2 GetFieldClickPosition()
        {
            var rect = _control.Rect;

            if (_control.Type == ControlType.Slider)
            {
                // Slider has a numeric text field on the right edge
                float fieldWidth = EditorGUIUtility.fieldWidth;
                return new Vector2(rect.xMax - fieldWidth / 2f, rect.center.y);
            }

            // For standard EditorGUILayout controls, the input area is past the label
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldX = rect.x + labelWidth + (rect.width - labelWidth) / 2f;
            return new Vector2(fieldX, rect.center.y);
        }

        private void EnsureReadableType()
        {
            switch (_control.Type)
            {
                case ControlType.TextField:
                case ControlType.IntField:
                case ControlType.FloatField:
                case ControlType.Slider:
                    return;
                default:
                    throw new InvalidOperationException(
                        $"ReadText() is not supported for control type '{_control.Type}'. " +
                        "Use it on TextField, IntField, FloatField, or Slider.");
            }
        }

    }
}

