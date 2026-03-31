using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Captures screenshots of EditorWindows and saves them as PNG files.
    /// </summary>
    public static class ScreenshotCapture
    {
        /// <summary>
        /// Captures the contents of an <see cref="EditorWindow"/> as a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="window">The editor window to capture.</param>
        /// <returns>A new <see cref="Texture2D"/> containing the window's pixels. The caller is responsible for destroying it.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is <c>null</c>.</exception>
        public static Texture2D CaptureWindow(EditorWindow window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.Repaint();

            var position = window.position;
            int width = (int)position.width;
            int height = (int)position.height;

            var pixels = InternalEditorUtility.ReadScreenPixel(
                new Vector2(position.x, position.y), width, height);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Captures the contents of an <see cref="EditorWindow"/> and saves the result as a PNG file.
        /// Parent directories are created automatically if they do not exist.
        /// </summary>
        /// <param name="window">The editor window to capture.</param>
        /// <param name="filePath">The file path where the PNG will be saved.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is <c>null</c> or empty.</exception>
        public static void CaptureAndSave(EditorWindow window, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            var texture = CaptureWindow(window);
            try
            {
                var bytes = texture.EncodeToPNG();

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(filePath, bytes);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
