using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorDriver
{
    /// <summary>
    /// Wraps a custom inspector hosted in an <see cref="InspectorHostWindow"/>.
    /// Provides the same interaction surface as <see cref="WindowHandle"/>
    /// while managing the lifecycle of the backing GameObject, Component, and Editor.
    /// </summary>
    public class InspectorHandle
    {
        private readonly WindowHandle _windowHandle;
        private readonly GameObject _gameObject;
        private readonly Editor _editor;

        internal InspectorHandle(EditorWindow hostWindow, GameObject gameObject, Editor editor)
        {
            _windowHandle = new WindowHandle(hostWindow);
            _gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>The host EditorWindow rendering the inspector.</summary>
        public EditorWindow Window => _windowHandle.Window;

        /// <summary>The custom <see cref="UnityEditor.Editor"/> being tested.</summary>
        public Editor Editor => _editor;

        /// <summary>The target Component of the inspector.</summary>
        public Component Component => _editor.target as Component;

        /// <summary>The GameObject holding the target Component.</summary>
        public GameObject GameObject => _gameObject;

        /// <summary>Sends a mouse click at the given window-local coordinates.</summary>
        public void ClickAt(float x, float y) => _windowHandle.ClickAt(x, y);

        /// <summary>Types text into the focused control of this window.</summary>
        public void TypeText(string text) => _windowHandle.TypeText(text);

        /// <summary>Sends a key press (down + up) to this window.</summary>
        public void SendKey(KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
            => _windowHandle.SendKey(keyCode, modifiers);

        /// <summary>Captures a screenshot and saves it as PNG.</summary>
        public void Screenshot(string filePath) => _windowHandle.Screenshot(filePath);

        /// <summary>
        /// Forces the window to repaint and synchronizes UIElements bindings.
        /// <para>
        /// On some platforms (notably Linux), UIElements binding updates can take
        /// several frames to propagate after <see cref="UnityEditor.SerializedObject.Update"/>.
        /// This method forces all bindings on the hosted visual tree to read their
        /// latest values immediately, so that subsequent <see cref="Page.Scan"/> or
        /// <see cref="Locator.ReadText"/> calls see up-to-date UI state.
        /// </para>
        /// </summary>
        public void Repaint()
        {
            SyncBindings(_windowHandle.Window);
            _windowHandle.Repaint();
        }

        /// <summary>
        /// Walks the visual tree and forces every <see cref="IBinding"/> to
        /// re-read its backing data (e.g. <see cref="UnityEditor.SerializedProperty"/>).
        /// </summary>
        private static void SyncBindings(EditorWindow window)
        {
            var root = window?.rootVisualElement;
            if (root == null) return;
            SyncBindingsRecursive(root);
        }

        private static void SyncBindingsRecursive(VisualElement element)
        {
            if (element is IBindable bindable)
            {
                var binding = bindable.binding;
                if (binding != null)
                {
                    binding.PreUpdate();
                    binding.Update();
                }
            }

            foreach (var child in element.Children())
                SyncBindingsRecursive(child);
        }

        /// <summary>
        /// Reads a field value from the underlying Component via reflection.
        /// </summary>
        public T GetFieldValue<T>(string fieldName)
        {
            var field = FindComponentField(fieldName);
            return (T)field.GetValue(_editor.target);
        }

        /// <summary>
        /// Writes a field value on the underlying Component via reflection.
        /// </summary>
        public void SetFieldValue<T>(string fieldName, T value)
        {
            var field = FindComponentField(fieldName);
            field.SetValue(_editor.target, value);
        }

        private FieldInfo FindComponentField(string fieldName)
        {
            var type = _editor.target.GetType();
            var field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(type.FullName, fieldName);
            return field;
        }
    }
}
