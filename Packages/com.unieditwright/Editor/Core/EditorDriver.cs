using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Main entry point for UniEditWright E2E testing.
    /// Manages editor window lifecycle.
    /// </summary>
    public class EditorDriver : IDisposable
    {
        private readonly List<EditorWindow> _openedWindows = new List<EditorWindow>();

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
        }

        public void Dispose()
        {
            CloseAll();
        }
    }
}
