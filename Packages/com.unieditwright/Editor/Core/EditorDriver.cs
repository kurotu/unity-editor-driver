using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Main entry point for UniEditWright E2E testing.
    /// Manages editor window and inspector lifecycle.
    /// </summary>
    public class EditorDriver : IDisposable
    {
        private readonly List<EditorWindow> _openedWindows = new List<EditorWindow>();
        private readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        public WindowHandle OpenWindow<T>() where T : EditorWindow
        {
            // Always create a fresh instance so each test starts with default field values.
            // GetWindow<T>() would return an existing (possibly stale) window from a
            // previous test, causing tests to see unexpected initial state.
            // ShowUtility() makes it a floating non-dockable window so that
            // window.position = ... takes effect immediately for resize tests.
            var window = ScriptableObject.CreateInstance<T>();
            window.ShowUtility();
            _openedWindows.Add(window);
            return new WindowHandle(window);
        }

        /// <summary>
        /// Opens a custom inspector for a Component in an isolated host window.
        /// Creates a temporary GameObject, adds <typeparamref name="TComponent"/>,
        /// and lets Unity select the default custom Editor.
        /// </summary>
        public InspectorHandle OpenInspector<TComponent>() where TComponent : Component
        {
            return OpenInspectorInternal<TComponent>(null);
        }

        /// <summary>
        /// Opens a specific custom inspector for a Component in an isolated host window.
        /// Creates a temporary GameObject, adds <typeparamref name="TComponent"/>,
        /// and uses <typeparamref name="TEditor"/> as the inspector.
        /// </summary>
        public InspectorHandle OpenInspector<TComponent, TEditor>()
            where TComponent : Component
            where TEditor : Editor
        {
            return OpenInspectorInternal<TComponent>(typeof(TEditor));
        }

        public void CloseInspector(InspectorHandle handle)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            var window = handle.Window;
            var go = handle.GameObject;
            _openedWindows.Remove(window);
            _createdGameObjects.Remove(go);
            if (window != null)
            {
                window.Close();
            }
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public void CloseWindow(WindowHandle handle)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            var window = handle.Window;
            _openedWindows.Remove(window);
            if (window != null)
            {
                window.Close();
            }
        }

        public void CloseAll()
        {
            for (int i = _openedWindows.Count - 1; i >= 0; i--)
            {
                var window = _openedWindows[i];
                if (window != null)
                {
                    window.Close();
                }
            }
            _openedWindows.Clear();

            for (int i = _createdGameObjects.Count - 1; i >= 0; i--)
            {
                var go = _createdGameObjects[i];
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            _createdGameObjects.Clear();
        }

        public void Dispose()
        {
            CloseAll();
        }

        private InspectorHandle OpenInspectorInternal<TComponent>(Type editorType)
            where TComponent : Component
        {
            var go = new GameObject($"UniEditWright_Inspector_{typeof(TComponent).Name}");
            // Use HideInHierarchy | DontSave instead of HideAndDontSave.
            // HideAndDontSave includes NotEditable, which makes SerializedProperty.editable
            // return false. UIElements BindProperty then disables bound controls, breaking
            // UIElements-based inspectors. IMGUI inspectors are unaffected because EditorGUI
            // APIs do not check the editable flag.
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _createdGameObjects.Add(go);

            var component = go.AddComponent<TComponent>();

            var editor = editorType != null
                ? Editor.CreateEditor(component, editorType)
                : Editor.CreateEditor(component);

            var hostWindow = ScriptableObject.CreateInstance<InspectorHostWindow>();
            hostWindow.SetEditor(editor);
            hostWindow.ShowUtility();
            _openedWindows.Add(hostWindow);

            return new InspectorHandle(hostWindow, go, editor);
        }
    }
}
