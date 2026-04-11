using UnityEditor;
using UnityEngine;

namespace EditorDriver
{
    /// <summary>
    /// Cross-platform helpers for <see cref="EditorWindow"/>.
    /// </summary>
    public static class EditorWindowExtensions
    {
        /// <summary>
        /// Sets the window position and size reliably across platforms.
        /// <para>
        /// On Linux (X11/Wayland), the <see cref="EditorWindow.position"/> setter
        /// sends an asynchronous resize request to the window manager.  The getter
        /// may return the old value until the WM processes the request, which can
        /// break code that reads the position back immediately.
        /// </para>
        /// <para>
        /// This method forces the resize by temporarily constraining
        /// <see cref="EditorWindow.minSize"/> and <see cref="EditorWindow.maxSize"/>
        /// to the desired dimensions, which makes the window manager apply the
        /// change synchronously on all platforms.
        /// </para>
        /// </summary>
        public static void SetPosition(this EditorWindow window, Rect rect)
        {
            var savedMin = window.minSize;
            var savedMax = window.maxSize;

            window.minSize = new Vector2(rect.width, rect.height);
            window.maxSize = new Vector2(rect.width, rect.height);
            window.position = rect;

            window.minSize = savedMin;
            window.maxSize = savedMax;
        }
    }
}
