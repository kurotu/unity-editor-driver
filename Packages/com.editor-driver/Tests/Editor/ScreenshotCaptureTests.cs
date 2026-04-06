using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EditorDriver.Tests
{
    [TestFixture]
    public class ScreenshotCaptureTests
    {
        private EditorWindow _window;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _window = ScriptableObject.CreateInstance<TestCaptureWindow>();
            _window.Show();

            _tempDir = Path.Combine(Path.GetTempPath(), "EditorDriver_ScreenshotTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
                _window.Close();

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public void CaptureWindow_ReturnsNonNullTexture()
        {
            var texture = ScreenshotCapture.CaptureWindow(_window);
            try
            {
                Assert.IsNotNull(texture);
            }
            finally
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CaptureWindow_TextureHasPositiveSize()
        {
            var texture = ScreenshotCapture.CaptureWindow(_window);
            try
            {
                Assert.Greater(texture.width, 0);
                Assert.Greater(texture.height, 0);
            }
            finally
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CaptureWindow_NullWindowThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ScreenshotCapture.CaptureWindow(null));
        }

        [Test]
        public void CaptureAndSave_CreatesFile()
        {
            var filePath = Path.Combine(_tempDir, "test_capture.png");

            ScreenshotCapture.CaptureAndSave(_window, filePath);

            Assert.IsTrue(File.Exists(filePath), "Screenshot file should exist after CaptureAndSave");
        }

        [Test]
        public void CaptureAndSave_FileIsPng()
        {
            var filePath = Path.Combine(_tempDir, "test_png.png");

            ScreenshotCapture.CaptureAndSave(_window, filePath);

            var bytes = File.ReadAllBytes(filePath);
            // PNG magic bytes: 137 80 78 71 (0x89 0x50 0x4E 0x47)
            Assert.GreaterOrEqual(bytes.Length, 4, "File should have at least 4 bytes");
            Assert.AreEqual(0x89, bytes[0]);
            Assert.AreEqual(0x50, bytes[1]);
            Assert.AreEqual(0x4E, bytes[2]);
            Assert.AreEqual(0x47, bytes[3]);
        }

        [Test]
        public void CaptureAndSave_NullPathThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ScreenshotCapture.CaptureAndSave(_window, null));
        }

        [Test]
        public void CaptureAndSave_EmptyPathThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ScreenshotCapture.CaptureAndSave(_window, ""));
        }

        [Test]
        public void CaptureAndSave_CreatesDirectoryIfNotExists()
        {
            var subDir = Path.Combine(_tempDir, "nested", "subdir");
            var filePath = Path.Combine(subDir, "screenshot.png");

            // The directory should not exist yet
            Assert.IsFalse(Directory.Exists(subDir));

            ScreenshotCapture.CaptureAndSave(_window, filePath);

            Assert.IsTrue(File.Exists(filePath), "File should be created even when parent directory didn't exist");
        }

        private class TestCaptureWindow : EditorWindow { }
    }
}
