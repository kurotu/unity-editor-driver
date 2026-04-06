using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using EditorDriver;

namespace EditorDriver.Tests
{
    /// <summary>
    /// E2E tests verifying bidirectional state synchronization for MyComponent's
    /// IMGUI custom inspector.
    ///
    /// UI → State: Operate controls via Page API, verify Component fields changed.
    /// State → UI: Set Component fields directly, verify UI displays new values.
    /// </summary>
    [TestFixture]
    public class MyComponentInspectorStateE2ETests
    {
        private Driver _driver;

        [SetUp]
        public void SetUp()
        {
            _driver = new Driver();
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
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByValue("Hello World").Fill("Inspector State");
            handle.Repaint();
            yield return null;

            var component = (MyComponent)handle.Component;
            Assert.AreEqual("Inspector State", component.myString);
        }

        // ── UI → State: ToggleGroup ─────────────────────────────────

        [UnityTest]
        public IEnumerator ToggleGroup_UpdatesComponentGroupEnabled()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var component = (MyComponent)handle.Component;
            Assert.IsFalse(component.groupEnabled,
                "groupEnabled should be false initially");

            var page = Page.Scan(handle.Window);
            page.GetByType(ControlType.Toggle, 1).Toggle();
            handle.Repaint();
            yield return null;

            Assert.IsTrue(component.groupEnabled,
                "groupEnabled should be true after toggling the group");
        }

        // ── UI → State: Slider ──────────────────────────────────────

        [UnityTest]
        public IEnumerator EnableGroupThenFillSlider_UpdatesComponentMyFloat()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var component = (MyComponent)handle.Component;
            Assert.AreEqual(1.23f, component.myFloat, 0.01f);

            var page = Page.Scan(handle.Window);
            page.GetByType(ControlType.Toggle, 1).Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();
            page.GetByValue("1.23").Fill("2.5");
            handle.Repaint();
            yield return null;

            Assert.AreEqual(2.5f, component.myFloat, 0.01f);
        }

        // ── State → UI: TextField ───────────────────────────────────

        [UnityTest]
        public IEnumerator SetComponentMyString_ReflectedInUI()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var component = (MyComponent)handle.Component;
            component.myString = "From Code";
            handle.Repaint();
            yield return null;

            var page = Page.Scan(handle.Window);
            var text = page.GetByValue("From Code").ReadText();
            Assert.AreEqual("From Code", text);
        }

        // ── State → UI: ToggleGroup ─────────────────────────────────

        [UnityTest]
        public IEnumerator SetComponentGroupEnabled_InnerControlsBecomeDiscoverable()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            // Initially inner controls are not discoverable
            var page = Page.Scan(handle.Window);
            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByValue("1.23"),
                "Slider inside disabled group should not be discoverable initially");

            // Enable group via component field
            var component = (MyComponent)handle.Component;
            component.groupEnabled = true;
            handle.Repaint();
            yield return null;

            page.Refresh();

            var slider = page.GetByValue("1.23");
            Assert.IsNotNull(slider, "Slider should be discoverable after enabling group");
        }

        // ── State → UI: Slider ──────────────────────────────────────

        [UnityTest]
        public IEnumerator SetComponentMyFloat_SliderReflectsNewValue()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var component = (MyComponent)handle.Component;
            component.groupEnabled = true;
            component.myFloat = 2.75f;
            handle.Repaint();
            yield return null;

            var page = Page.Scan(handle.Window);
            var sliderText = page.GetByValue("2.75").ReadText();
            Assert.AreEqual("2.75", sliderText);
        }

        // ── Full round-trip ─────────────────────────────────────────

        [UnityTest]
        public IEnumerator FullRoundTrip_UIToStateAndBack()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var component = (MyComponent)handle.Component;

            // 1. Verify initial state
            Assert.AreEqual("Hello World", component.myString);
            Assert.IsFalse(component.groupEnabled);

            // 2. Edit text via UI → verify state
            var page = Page.Scan(handle.Window);
            page.GetByValue("Hello World").Fill("Round Trip");
            handle.Repaint();
            yield return null;

            Assert.AreEqual("Round Trip", component.myString);

            // 3. Set state → verify UI
            component.myString = "Back Again";
            handle.Repaint();
            yield return null;

            page.Refresh();
            var readBack = page.GetByValue("Back Again").ReadText();
            Assert.AreEqual("Back Again", readBack);
        }

    }
}
