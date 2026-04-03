using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniEditWright
{
    /// <summary>
    /// A lightweight EditorWindow that hosts a single <see cref="UnityEditor.Editor"/>
    /// in isolation. Supports both IMGUI (<see cref="UnityEditor.Editor.OnInspectorGUI"/>)
    /// and UIElements (<see cref="UnityEditor.Editor.CreateInspectorGUI"/>) inspectors.
    /// This allows the existing Page/Locator/InputSimulator stack to test custom inspectors
    /// without any modifications — they only need an <see cref="EditorWindow"/>.
    /// </summary>
    internal sealed class InspectorHostWindow : EditorWindow
    {
        private Editor _editor;
        private Vector2 _scrollPosition;
        private bool _isUIElements;

        internal Editor HostedEditor => _editor;

        internal void SetEditor(Editor editor)
        {
            _editor = editor;

            // Try UIElements inspector first; fall back to IMGUI in OnGUI
            var inspectorElement = editor.CreateInspectorGUI();
            if (inspectorElement != null)
            {
                _isUIElements = true;
                var scrollView = new ScrollView(ScrollViewMode.Vertical);
                scrollView.style.flexGrow = 1;
                scrollView.Add(inspectorElement);
                rootVisualElement.Add(scrollView);
            }
        }

        private void OnGUI()
        {
            if (_isUIElements) return;
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
