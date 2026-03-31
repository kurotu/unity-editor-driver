using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UniEditWright.Tests
{
    [TestFixture]
    public class EditorDriverTests
    {
        private EditorDriver _driver;

        [SetUp]
        public void SetUp()
        {
            _driver = new EditorDriver();
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose cleans up any windows the driver opened
            _driver?.Dispose();
        }

        [Test]
        public void OpenWindow_ReturnsNonNullHandle()
        {
            var handle = _driver.OpenWindow<TestDriverWindow>();

            Assert.IsNotNull(handle);
        }

        [Test]
        public void OpenWindow_WindowIsVisible()
        {
            var handle = _driver.OpenWindow<TestDriverWindow>();

            // A visible EditorWindow should report hasFocus or at minimum have a non-zero position
            Assert.IsNotNull(handle.Window, "WindowHandle.Window should reference the opened window");
            Assert.IsTrue(handle.Window.position.width > 0 || handle.Window.position.height > 0,
                "Window should have a non-zero size when visible");
        }

        [Test]
        public void OpenWindow_HandleReferencesCorrectWindowType()
        {
            var handle = _driver.OpenWindow<TestDriverWindow>();

            Assert.IsInstanceOf<TestDriverWindow>(handle.Window);
        }

        [Test]
        public void CloseWindow_DestroysWindow()
        {
            var handle = _driver.OpenWindow<TestDriverWindow>();
            var window = handle.Window;

            _driver.CloseWindow(handle);

            // Unity uses a custom null check for destroyed objects
            Assert.IsTrue(window == null, "Window should be destroyed after CloseWindow");
        }

        [Test]
        public void CloseAll_ClosesAllOpenedWindows()
        {
            var handle1 = _driver.OpenWindow<TestDriverWindow>();
            var handle2 = _driver.OpenWindow<TestDriverWindowB>();
            var win1 = handle1.Window;
            var win2 = handle2.Window;

            _driver.CloseAll();

            Assert.IsTrue(win1 == null, "First window should be destroyed");
            Assert.IsTrue(win2 == null, "Second window should be destroyed");
        }

        [Test]
        public void Dispose_ClosesAllWindows()
        {
            WindowHandle handle;
            EditorWindow window;

            using (var driver = new EditorDriver())
            {
                handle = driver.OpenWindow<TestDriverWindow>();
                window = handle.Window;
            }

            Assert.IsTrue(window == null, "Window should be destroyed after Dispose");
        }

        private class TestDriverWindow : EditorWindow { }
        private class TestDriverWindowB : EditorWindow { }
    }
}
