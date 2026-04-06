using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using EditorDriver;

namespace EditorDriver.Tests
{
    /// <summary>
    /// End-to-end integration test using MyUIElementsWindow as the test target.
    /// Demonstrates UIElements (UI Toolkit) window testing via Page.Scan().
    ///
    /// Unlike IMGUI windows where only value-based lookup is available via Scan,
    /// UIElements Scan also provides label-based lookup because control labels
    /// are part of the visual tree.
    ///
    /// MyUIElementsWindow initial state:
    ///   - TextField "Text Field": "Hello World"
    ///   - Toggle "Optional Settings": disabled — inner controls are inactive
    ///   - Toggle "Toggle" / Slider "Slider" inside group: NOT discoverable until group is enabled
    /// </summary>
    [TestFixture]
    public class MyUIElementsWindowE2ETests
    {
        private Driver _driver;
        private string _screenshotDir;

        [SetUp]
        public void SetUp()
        {
            _driver = new Driver();
            _screenshotDir = "EditorDriver_E2E_" + System.Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(_screenshotDir);
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Dispose();
            if (Directory.Exists(_screenshotDir))
                Directory.Delete(_screenshotDir, recursive: true);
        }

        // ── Screenshot ──────────────────────────────────────────────

        [UnityTest]
        public IEnumerator OpenMyWindow_TakeScreenshot_WindowIsVisible()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            Assert.IsNotNull(handle.Window);
            Assert.Greater(handle.Window.position.width, 0);

            var path = Path.Combine(_screenshotDir, "uielements_window_initial.png");
            handle.Screenshot(path);
            Assert.IsTrue(File.Exists(path), "Screenshot should be saved");
        }

        // ── Scan — Label-based lookup (UIElements advantage) ────────

        [UnityTest]
        public IEnumerator Scan_FindTextFieldByLabel_ReturnsLocator()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            // UIElements Scan discovers labels from the visual tree
            var page = Page.Scan(handle.Window);
            var locator = page.GetByLabel("Text Field");

            Assert.AreEqual(ControlType.TextField, locator.ControlType);
            Assert.Greater(locator.Rect.width, 0, "TextField should have positive width");
            Assert.Greater(locator.Rect.height, 0, "TextField should have positive height");
        }

        // ── Scan — Value-based lookup ───────────────────────────────

        [UnityTest]
        public IEnumerator Scan_FindTextFieldByValue_ReturnsDisplayedValue()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var locator = page.GetByValue("Hello World");

            Assert.AreEqual(ControlType.TextField, locator.ControlType);
        }

        [UnityTest]
        public IEnumerator Scan_ReadTextField_ReturnsDisplayedValue()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var text = page.GetByLabel("Text Field").ReadText();

            Assert.AreEqual("Hello World", text);
        }

        // ── Fill ────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_FillTextField_ThenReadBack()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByLabel("Text Field").Fill("E2E UIElements Test");
            handle.Repaint();
            yield return null;

            // Re-scan to pick up the new value from the visual tree
            page.Refresh();
            var readBack = page.GetByValue("E2E UIElements Test").ReadText();
            Assert.AreEqual("E2E UIElements Test", readBack);
        }

        // ── Toggle group ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_InitialState_DisabledGroupControlsNotDiscoverable()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            // Slider inside disabled group should not be discoverable
            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByLabel("Slider"),
                "Slider inside disabled group should not be discoverable");
        }

        [UnityTest]
        public IEnumerator Scan_EnableToggleGroup_RevealedInnerControls()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            int countBefore = page.Controls.Count;

            // Enable the toggle group
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();

            Assert.Greater(page.Controls.Count, countBefore,
                "Enabling toggle group should reveal inner controls");
        }

        [UnityTest]
        public IEnumerator Scan_AfterEnablingGroup_SliderIsDiscoverable()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();

            var slider = page.GetByLabel("Slider");
            Assert.IsNotNull(slider);
            Assert.AreEqual(ControlType.Slider, slider.ControlType);
            Assert.Greater(slider.Rect.width, 0);
        }

        // ── Describe (manual descriptor) ────────────────────────────

        [UnityTest]
        public IEnumerator Describe_FindTextField_ValidRect()
        {
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            // Page.Describe also works with UIElements — auto-detects the resolver
            var page = Page.Describe(handle.Window, p =>
            {
                p.Label("Base Settings");
                p.TextField("Text Field");
                p.Toggle("Optional Settings");
            });

            var locator = page.GetByLabel("Text Field");
            Assert.Greater(locator.Rect.width, 0, "TextField should have positive width");
            Assert.Greater(locator.Rect.height, 0, "TextField should have positive height");
        }

        // ── Full workflow ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator FullWorkflow_BlackBox()
        {
            // 1. Open window
            var handle = _driver.OpenWindow<MyUIElementsWindow>();
            yield return null;

            // 2. Scan — both labels and values available for UIElements
            var page = Page.Scan(handle.Window);
            handle.Screenshot(Path.Combine(_screenshotDir, "step1_initial.png"));

            // 3. Verify initial value via both label and value lookup
            Assert.AreEqual("Hello World", page.GetByValue("Hello World").ReadText());

            // 4. Edit text field
            page.GetByLabel("Text Field").Fill("UIElements Black Box");
            handle.Repaint();
            yield return null;

            page.Refresh();
            Assert.AreEqual("UIElements Black Box",
                page.GetByValue("UIElements Black Box").ReadText());

            // 5. Enable toggle group
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();
            handle.Screenshot(Path.Combine(_screenshotDir, "step2_group_enabled.png"));

            // 6. Inner slider is now accessible by label
            var slider = page.GetByLabel("Slider");
            Assert.IsNotNull(slider);
            Assert.IsTrue(File.Exists(Path.Combine(_screenshotDir, "step2_group_enabled.png")));

            // 7. Close
            _driver.CloseAll();
        }
    }
}
