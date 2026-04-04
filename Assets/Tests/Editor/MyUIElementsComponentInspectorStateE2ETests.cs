using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    /// <summary>
    /// E2E tests verifying bidirectional state synchronization for
    /// MyUIElementsComponent's UIElements inspector (uses BindProperty).
    ///
    /// UI → State: Operate controls via Page API, verify Component fields changed.
    /// State → UI: Set Component fields directly, verify UI displays new values.
    ///
    /// Since the inspector uses <c>BindProperty</c>, the SerializedObject must be
    /// updated after direct field changes for the binding to pick up new values.
    /// </summary>
    [TestFixture]
    public class MyUIElementsComponentInspectorStateE2ETests
    {
        private EditorDriver _driver;

        /// <summary>
        /// Yields several frames so UIElements bindings resolve and the
        /// internal TextInput registers its command-event handlers.
        /// </summary>
        private static IEnumerator WaitForBindings()
        {
            yield return null;
            yield return null;
            yield return null;
        }

        [SetUp]
        public void SetUp()
        {
            _driver = new EditorDriver();
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Dispose();
        }

        // ── UI → State: TextField ───────────────────────────────────

        [UnityTest]
        public IEnumerator FillTextField_UpdatesComponentField()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            var page = Page.Scan(handle.Window);
            page.GetByLabel("Text Field").Fill("UIElements State");
            handle.Repaint();
            yield return WaitForBindings();

            var component = (MyUIElementsComponent)handle.Component;
            Assert.AreEqual("UIElements State", component.myString);
        }

        // ── UI → State: ToggleGroup ─────────────────────────────────

        [UnityTest]
        public IEnumerator ToggleGroup_UpdatesComponentGroupEnabled()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            var component = (MyUIElementsComponent)handle.Component;
            Assert.IsFalse(component.groupEnabled,
                "groupEnabled should be false initially");

            var page = Page.Scan(handle.Window);
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return WaitForBindings();

            Assert.IsTrue(component.groupEnabled,
                "groupEnabled should be true after toggling");
        }

        // ── UI → State: Inner Toggle ────────────────────────────────

        [UnityTest]
        public IEnumerator EnableGroupThenToggle_UpdatesComponentMyBool()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            var component = (MyUIElementsComponent)handle.Component;
            Assert.IsTrue(component.myBool, "myBool should be true initially");

            var page = Page.Scan(handle.Window);

            // Enable group
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return WaitForBindings();

            // Re-scan to discover inner controls
            page.Refresh();

            // Toggle the inner toggle
            page.GetByLabel("Toggle").Toggle();
            handle.Repaint();
            yield return WaitForBindings();

            Assert.IsFalse(component.myBool,
                "myBool should be false after toggling the inner toggle");
        }

        // ── State → UI: TextField ───────────────────────────────────

        [UnityTest]
        public IEnumerator SetComponentMyString_ReflectedInUI()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            var component = (MyUIElementsComponent)handle.Component;
            component.myString = "From Code";

            // Notify the binding system of the change
            handle.Editor.serializedObject.Update();
            handle.Repaint();
            yield return WaitForBindings();

            var page = Page.Scan(handle.Window);
            var text = page.GetByLabel("Text Field").ReadText();
            Assert.AreEqual("From Code", text);
        }

        // ── State → UI: ToggleGroup ─────────────────────────────────

        [UnityTest]
        public IEnumerator SetComponentGroupEnabled_InnerControlsBecomeDiscoverable()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            // Initially inner controls are not discoverable
            var page = Page.Scan(handle.Window);
            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByLabel("Slider"),
                "Slider inside disabled group should not be discoverable initially");

            // Enable group via component field
            var component = (MyUIElementsComponent)handle.Component;
            component.groupEnabled = true;

            handle.Editor.serializedObject.Update();
            handle.Repaint();
            yield return WaitForBindings();

            page.Refresh();

            var slider = page.GetByLabel("Slider");
            Assert.IsNotNull(slider, "Slider should be discoverable after enabling group");
        }

        // ── Full round-trip ─────────────────────────────────────────

        [UnityTest]
        public IEnumerator FullRoundTrip_UIToStateAndBack()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            var component = (MyUIElementsComponent)handle.Component;

            // 1. Verify initial state
            Assert.AreEqual("Hello World", component.myString);
            Assert.IsFalse(component.groupEnabled);

            // 2. Edit text via UI → verify state
            var page = Page.Scan(handle.Window);
            page.GetByLabel("Text Field").Fill("Round Trip");
            handle.Repaint();
            yield return WaitForBindings();

            Assert.AreEqual("Round Trip", component.myString);

            // 3. Set state → verify UI
            component.myString = "Back Again";
            handle.Editor.serializedObject.Update();
            handle.Repaint();
            yield return WaitForBindings();

            page.Refresh();
            var readBack = page.GetByValue("Back Again").ReadText();
            Assert.AreEqual("Back Again", readBack);
        }
    }
}
