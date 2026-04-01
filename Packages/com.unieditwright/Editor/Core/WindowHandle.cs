using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Wraps an EditorWindow and provides interaction methods.
    /// </summary>
    public class WindowHandle
    {
        private readonly EditorWindow _window;

        public WindowHandle(EditorWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public EditorWindow Window => _window;

        /// <summary>
        /// Sends a mouse click at the given window-local coordinates.
        /// </summary>
        public void ClickAt(float x, float y)
        {
            InputSimulator.MouseClick(_window, new Vector2(x, y));
        }

        /// <summary>
        /// Types text into the focused control of this window.
        /// </summary>
        public void TypeText(string text)
        {
            InputSimulator.TypeText(_window, text);
        }

        /// <summary>
        /// Sends a key press (down + up) to this window.
        /// </summary>
        public void SendKey(KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
        {
            InputSimulator.KeyDown(_window, keyCode, modifiers);
            InputSimulator.KeyUp(_window, keyCode, modifiers);
        }

        /// <summary>
        /// Captures a screenshot of this window and saves it as PNG.
        /// </summary>
        public void Screenshot(string filePath)
        {
            ScreenshotCapture.CaptureAndSave(_window, filePath);
        }

        /// <summary>
        /// Reads a field value from the underlying window via reflection.
        /// </summary>
        public T GetFieldValue<T>(string fieldName)
        {
            var field = FindField(fieldName);
            return (T)field.GetValue(_window);
        }

        /// <summary>
        /// Writes a field value on the underlying window via reflection.
        /// </summary>
        public void SetFieldValue<T>(string fieldName, T value)
        {
            var field = FindField(fieldName);
            field.SetValue(_window, value);
        }

        /// <summary>
        /// Forces the window to repaint.
        /// </summary>
        public void Repaint()
        {
            _window.Repaint();
        }

        private FieldInfo FindField(string fieldName)
        {
            var type = _window.GetType();
            var field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(type.FullName, fieldName);
            }
            return field;
        }
    }
}
