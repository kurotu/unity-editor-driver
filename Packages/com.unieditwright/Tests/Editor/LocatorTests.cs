using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    [TestFixture]
    public class LocatorTests
    {
        private TestTrackedWindow _trackedWindow;

        [SetUp]
        public void SetUp()
        {
            _trackedWindow = EditorWindow.GetWindow<TestTrackedWindow>();
            _trackedWindow.Show();
        }

        [TearDown]
        public void TearDown()
        {
            if (_trackedWindow != null)
                _trackedWindow.Close();
        }

        [Test]
        public void Constructor_SetsLabel()
        {
            var window = ScriptableObject.CreateInstance<TestLocatorWindow>();
            try
            {
                var locator = new Locator(window, "My Label");

                Assert.AreEqual("My Label", locator.Label);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void Constructor_NullWindowThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Locator(null, "label"));
        }

        [Test]
        public void Constructor_NullLabelThrowsArgumentNull()
        {
            var window = ScriptableObject.CreateInstance<TestLocatorWindow>();
            try
            {
                Assert.Throws<ArgumentNullException>(() => new Locator(window, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [UnityTest]
        public IEnumerator Click_SendsMouseEventToControlRect()
        {
            // Wait for OnGUI to run so the tracker records entries
            yield return null;

            var locator = new Locator(_trackedWindow, "Check");

            // Should dispatch a click at the control's rect center; verify no exception
            Assert.DoesNotThrow(() => locator.Click());
        }

        [UnityTest]
        public IEnumerator Fill_TypesTextIntoControl()
        {
            yield return null;

            var locator = new Locator(_trackedWindow, "Text");

            Assert.DoesNotThrow(() => locator.Fill("new value"));
        }

        [UnityTest]
        public IEnumerator GetRect_ReturnsControlRect()
        {
            yield return null;

            var locator = new Locator(_trackedWindow, "Text");
            var rect = locator.GetRect();

            // The rect should have non-zero dimensions from the tracked TextField
            Assert.Greater(rect.width, 0f, "Control rect width should be positive");
            Assert.Greater(rect.height, 0f, "Control rect height should be positive");
        }

        [UnityTest]
        public IEnumerator GetRect_ThrowsIfControlNotFound()
        {
            yield return null;

            var locator = new Locator(_trackedWindow, "NonExistentControl");

            Assert.Throws<InvalidOperationException>(() => locator.GetRect());
        }

        private class TestLocatorWindow : EditorWindow { }
    }
}
