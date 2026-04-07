using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorDriver
{
    /// <summary>
    /// A lightweight EditorWindow that hosts a single <see cref="UnityEditor.Editor"/>
    /// in isolation. Supports both IMGUI (<see cref="UnityEditor.Editor.OnInspectorGUI"/>)
    /// and UIElements (<see cref="UnityEditor.Editor.CreateInspectorGUI"/>) inspectors.
    /// This allows the existing Page/Locator/InputSimulator stack to test custom inspectors
    /// without any modifications — they only need an <see cref="EditorWindow"/>.
    /// </summary>
    /// <remarks>
    /// This class intentionally does NOT override <c>OnGUI</c>.  When a subclass
    /// overrides OnGUI, Unity creates a "root IMGUI container" that intercepts
    /// <c>SendEvent</c> calls.  For UIElements inspectors, this prevents command
    /// events (SelectAll, Copy, Paste) from reaching UIElements controls.
    /// Instead, IMGUI inspectors are rendered via an <see cref="IMGUIContainer"/>
    /// added to <see cref="EditorWindow.rootVisualElement"/>.  This keeps the
    /// visual-tree event path clear for both UIElements and IMGUI inspectors.
    /// </remarks>
    internal sealed class InspectorHostWindow : EditorWindow
    {
        private Editor _editor;
        private Vector2 _scrollPosition;
        private VisualElement _pendingUIElements;
        private bool _pendingIMGUI;

        internal Editor HostedEditor => _editor;

        /// <summary>
        /// Configures the hosted editor.  The actual visual tree is built in
        /// <see cref="CreateGUI"/> so that UIElements content goes through
        /// Unity's standard panel-initialisation lifecycle — the same path
        /// that <see cref="EditorWindow.CreateGUI"/> takes for normal windows.
        /// This ensures focus management, bindings, and command-event routing
        /// (SelectAll, Copy, Paste) work correctly from the first frame.
        /// </summary>
        internal void SetEditor(Editor editor)
        {
            _editor = editor;

            var inspectorElement = editor.CreateInspectorGUI();
            if (inspectorElement != null)
            {
                _pendingUIElements = inspectorElement;
            }
            else
            {
                _pendingIMGUI = true;
            }
        }

        private void CreateGUI()
        {
            if (_pendingUIElements != null)
            {
                var scrollView = new ScrollView(ScrollViewMode.Vertical);
                scrollView.style.flexGrow = 1;
                scrollView.Add(_pendingUIElements);
                rootVisualElement.Add(scrollView);
                _pendingUIElements = null;
            }
            else if (_pendingIMGUI)
            {
                var imguiContainer = new IMGUIContainer(OnInspectorGUI);
                imguiContainer.style.flexGrow = 1;
                rootVisualElement.Add(imguiContainer);
                _pendingIMGUI = false;
            }
        }

        private void OnInspectorGUI()
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
