using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    /// <summary>
    /// End-to-end integration test using MyWindow as the test target.
    /// Fully black-box: no reflection into MyWindow's fields, no manual Page.Describe.
    /// Uses Page.Scan() for automatic control discovery.
    ///
    /// MyWindow initial state:
    ///   - TextField: "Hello World"
    ///   - Toggle (ToggleGroup): disabled — inner controls are inactive
    ///   - Toggle / Slider inside group: NOT discoverable until group is enabled
    /// </summary>
    [TestFixture]
    public class MyWindowE2ETests
    {
        private EditorDriver _driver;
        private string _screenshotDir;

        [SetUp]
        public void SetUp()
        {
            _driver = new EditorDriver();
            _screenshotDir = "UniEditWright_E2E_" + System.Guid.NewGuid().ToString("N");
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
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            Assert.IsNotNull(handle.Window);
            Assert.Greater(handle.Window.position.width, 0);

            var path = Path.Combine(_screenshotDir, "mywindow_initial.png");
            handle.Screenshot(path);
            Assert.IsTrue(File.Exists(path), "Screenshot should be saved");
        }

        // ── Read ────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_ReadTextField_ReturnsDisplayedValue()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var text = page.GetByValue("Hello World").ReadText();

            Assert.AreEqual("Hello World", text);
        }

        [UnityTest]
        public IEnumerator Scan_TextField_HasValidRect()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var locator = page.GetByValue("Hello World");

            Assert.AreEqual(ControlType.TextField, locator.ControlType);
            Assert.Greater(locator.Rect.width, 0, "TextField should have positive width");
            Assert.Greater(locator.Rect.height, 0, "TextField should have positive height");
        }

        // ── Fill ────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_FillTextField_ThenReadBack()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByValue("Hello World").Fill("E2E Test Value");
            handle.Repaint();
            yield return null;

            // Refresh to update discovered values
            page.Refresh();
            var readBack = page.GetByValue("E2E Test Value").ReadText();
            Assert.AreEqual("E2E Test Value", readBack);
        }

        // ── Toggle group ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_InitialState_InnerControlsNotDiscoverable()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            // Group is disabled initially — slider is not interactive, so Scan won't find "1.23"
            var page = Page.Scan(handle.Window);

            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByValue("1.23"),
                "Slider inside disabled group should not be discoverable");
        }

        [UnityTest]
        public IEnumerator Scan_EnableToggleGroup_RevealedInnerControls()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            int countBefore = page.Controls.Count;

            // Enable the toggle group
            page.GetByType(ControlType.Toggle, 1).Toggle();
            handle.Repaint();
            yield return null;

            // Re-scan to pick up newly active controls
            page.Refresh();

            Assert.Greater(page.Controls.Count, countBefore,
                "Enabling toggle group should reveal inner controls");
        }

        [UnityTest]
        public IEnumerator Scan_AfterEnablingGroup_SliderIsDiscoverable()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByType(ControlType.Toggle, 1).Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();

            // Slider numeric field shows "1.23" — discoverable after group is enabled
            var slider = page.GetByValue("1.23");
            Assert.IsNotNull(slider);
            Assert.Greater(slider.Rect.width, 0);
        }

        // ── ObjectField ──────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_ObjectField_IsDiscoverable()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            var objField = page.GetByType(ControlType.ObjectField, 0);
            Assert.IsNotNull(objField);
            Assert.AreEqual(ControlType.ObjectField, objField.ControlType);
            Assert.Greater(objField.Rect.width, 0);
            Assert.Greater(objField.Rect.height, 0);
        }

        [UnityTest]
        public IEnumerator Scan_ObjectField_ClickDoesNotThrow()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            Assert.DoesNotThrow(() =>
                page.GetByType(ControlType.ObjectField, 0).Click());
        }

        [UnityTest]
        public IEnumerator Scan_ObjectField_CaptureScreenshot()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var objField = page.GetByType(ControlType.ObjectField, 0);

            var tex = objField.CaptureScreenshot();
            Assert.IsNotNull(tex);
            Assert.Greater(tex.width, 0);
            Assert.Greater(tex.height, 0);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        // ── Full workflow ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator FullWorkflow_BlackBox()
        {
            // 1. Open window
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            // 2. Scan — no descriptor needed
            var page = Page.Scan(handle.Window);
            handle.Screenshot(Path.Combine(_screenshotDir, "step1_initial.png"));

            // 3. Read initial value
            Assert.AreEqual("Hello World", page.GetByValue("Hello World").ReadText());

            // 4. Edit text field
            page.GetByValue("Hello World").Fill("Black Box Test");
            handle.Repaint();
            yield return null;

            page.Refresh();
            Assert.AreEqual("Black Box Test", page.GetByValue("Black Box Test").ReadText());

            // 5. Enable toggle group
            page.GetByType(ControlType.Toggle, 1).Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();
            handle.Screenshot(Path.Combine(_screenshotDir, "step2_group_enabled.png"));

            // 6. Inner slider is now accessible
            var slider = page.GetByValue("1.23");
            Assert.IsNotNull(slider);
            Assert.IsTrue(File.Exists(Path.Combine(_screenshotDir, "step2_group_enabled.png")));

            // 7. Close
            _driver.CloseAll();
        }
    }
}
