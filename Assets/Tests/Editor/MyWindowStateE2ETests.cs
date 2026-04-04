using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    /// <summary>
    /// E2E tests verifying bidirectional state synchronization for MyWindow (IMGUI).
    ///
    /// UI → State: Operate controls via Page API, verify internal fields changed.
    /// State → UI: Set internal fields via reflection, verify UI displays new values.
    /// </summary>
    [TestFixture]
    public class MyWindowStateE2ETests
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
            _driver?.Dispose();
        }

        // ── UI → State: TextField ───────────────────────────────────

        [UnityTest]
        public IEnumerator FillTextField_UpdatesMyStringField()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByValue("Hello World").Fill("State Test");
            handle.Repaint();
            yield return null;

            Assert.AreEqual("State Test", handle.GetFieldValue<string>("myString"));
        }

        // ── UI → State: ToggleGroup ─────────────────────────────────

        [UnityTest]
        public IEnumerator ToggleGroup_UpdatesGroupEnabledField()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            Assert.IsFalse(handle.GetFieldValue<bool>("groupEnabled"),
                "groupEnabled should be false initially");

            var page = Page.Scan(handle.Window);
            page.GetByType(ControlType.Toggle, 1).Toggle();
            handle.Repaint();
            yield return null;

            Assert.IsTrue(handle.GetFieldValue<bool>("groupEnabled"),
                "groupEnabled should be true after toggling the group");
        }

        // ── UI → State: Slider ──────────────────────────────────────

        [UnityTest]
        public IEnumerator EnableGroupThenFillSlider_UpdatesMyFloatField()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            Assert.AreEqual(1.23f, handle.GetFieldValue<float>("myFloat"), 0.01f);

            var page = Page.Scan(handle.Window);
            page.GetByType(ControlType.Toggle, 1).Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();
            page.GetByValue("1.23").Fill("2.5");
            handle.Repaint();
            yield return null;

            Assert.AreEqual(2.5f, handle.GetFieldValue<float>("myFloat"), 0.01f);
        }

        // ── State → UI: TextField ───────────────────────────────────

        [UnityTest]
        public IEnumerator SetMyStringField_ReflectedInUI()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            handle.SetFieldValue("myString", "From Code");
            handle.Repaint();
            yield return null;

            var page = Page.Scan(handle.Window);
            var text = page.GetByValue("From Code").ReadText();
            Assert.AreEqual("From Code", text);
        }

        // ── State → UI: ToggleGroup ─────────────────────────────────

        [UnityTest]
        public IEnumerator SetGroupEnabled_InnerControlsBecomeDiscoverable()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            // Initially inner controls are not discoverable
            var page = Page.Scan(handle.Window);
            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByValue("1.23"),
                "Slider inside disabled group should not be discoverable initially");

            // Enable group via internal state
            handle.SetFieldValue("groupEnabled", true);
            handle.Repaint();
            yield return null;

            page.Refresh();

            var slider = page.GetByValue("1.23");
            Assert.IsNotNull(slider, "Slider should be discoverable after enabling group");
        }

        // ── State → UI: Slider ──────────────────────────────────────

        [UnityTest]
        public IEnumerator SetMyFloatField_SliderReflectsNewValue()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            // Enable group and set float value via internal state
            handle.SetFieldValue("groupEnabled", true);
            handle.SetFieldValue("myFloat", 2.75f);
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
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            // 1. Verify initial state
            Assert.AreEqual("Hello World", handle.GetFieldValue<string>("myString"));
            Assert.IsFalse(handle.GetFieldValue<bool>("groupEnabled"));

            // 2. Edit text via UI → verify state
            var page = Page.Scan(handle.Window);
            page.GetByValue("Hello World").Fill("Round Trip");
            handle.Repaint();
            yield return null;

            Assert.AreEqual("Round Trip", handle.GetFieldValue<string>("myString"));

            // 3. Set state → verify UI
            handle.SetFieldValue("myString", "Back Again");
            handle.Repaint();
            yield return null;

            page.Refresh();
            var readBack = page.GetByValue("Back Again").ReadText();
            Assert.AreEqual("Back Again", readBack);
        }

    }
}
