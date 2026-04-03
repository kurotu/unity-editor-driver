using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// A lightweight EditorWindow that hosts a single <see cref="UnityEditor.Editor"/>
    /// and renders its <see cref="UnityEditor.Editor.OnInspectorGUI"/> in isolation.
    /// This allows the existing Page/Locator/InputSimulator stack to test custom inspectors
    /// without any modifications — they only need an <see cref="EditorWindow"/>.
    /// </summary>
    internal sealed class InspectorHostWindow : EditorWindow
    {
        private Editor _editor;
        private Vector2 _scrollPosition;

        internal Editor HostedEditor => _editor;

        internal void SetEditor(Editor editor)
        {
            _editor = editor;
        }

        private void OnGUI()
        {
            if (_editor == null || _editor.target == null)
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _editor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void OnDestroy()
        {
            if (_editor != null)
            {
                DestroyImmediate(_editor);
                _editor = null;
            }
        }
    }
}
