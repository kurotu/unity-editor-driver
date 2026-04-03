using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    /// <summary>
    /// End-to-end integration test using MyComponent's custom inspector as the test target.
    /// Mirrors <see cref="MyWindowE2ETests"/> to verify the inspector testing path
    /// works identically to the EditorWindow path.
    ///
    /// MyComponent initial state (same as MyWindow):
    ///   - TextField: "Hello World"
    ///   - Toggle (ToggleGroup): disabled — inner controls are inactive
    ///   - Toggle / Slider inside group: NOT discoverable until group is enabled
    /// </summary>
    [TestFixture]
    public class MyComponentInspectorE2ETests
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

        // ── Open / Close ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator OpenInspector_ReturnsValidHandle()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            Assert.IsNotNull(handle);
            Assert.IsNotNull(handle.Window);
            Assert.IsNotNull(handle.Editor);
            Assert.IsNotNull(handle.Component);
            Assert.IsNotNull(handle.GameObject);
            Assert.Greater(handle.Window.position.width, 0);
        }

        [UnityTest]
        public IEnumerator OpenInspector_WithExplicitEditorType_UsesCorrectEditor()
        {
            var handle = _driver.OpenInspector<MyComponent, MyComponentEditor>();
            yield return null;

            Assert.IsInstanceOf<MyComponentEditor>(handle.Editor);
        }

        // ── Screenshot ──────────────────────────────────────────────

        [UnityTest]
        public IEnumerator OpenInspector_TakeScreenshot_WindowIsVisible()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var path = Path.Combine(_screenshotDir, "inspector_initial.png");
            handle.Screenshot(path);
            Assert.IsTrue(File.Exists(path), "Screenshot should be saved");
        }

        // ── Read ────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_ReadTextField_ReturnsDisplayedValue()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var text = page.GetByValue("Hello World").ReadText();

            Assert.AreEqual("Hello World", text);
        }

        [UnityTest]
        public IEnumerator Scan_TextField_HasValidRect()
        {
            var handle = _driver.OpenInspector<MyComponent>();
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
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByValue("Hello World").Fill("Inspector Test");
            handle.Repaint();
            yield return null;

            page.Refresh();
            var readBack = page.GetByValue("Inspector Test").ReadText();
            Assert.AreEqual("Inspector Test", readBack);
        }

        // ── Toggle group ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_InitialState_InnerControlsNotDiscoverable()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);

            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByValue("1.23"),
                "Slider inside disabled group should not be discoverable");
        }

        [UnityTest]
        public IEnumerator Scan_EnableToggleGroup_RevealedInnerControls()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);
            int countBefore = page.Controls.Count;

            page.GetByType(ControlType.Toggle, 0).Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();

            Assert.Greater(page.Controls.Count, countBefore,
                "Enabling toggle group should reveal inner controls");
        }

        [UnityTest]
        public IEnumerator Scan_AfterEnablingGroup_SliderIsDiscoverable()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);
            page.GetByType(ControlType.Toggle, 0).Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();

            var slider = page.GetByValue("1.23");
            Assert.IsNotNull(slider);
            Assert.Greater(slider.Rect.width, 0);
        }

        // ── Full workflow ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator FullWorkflow_BlackBox()
        {
            // 1. Open inspector
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            // 2. Scan — no descriptor needed
            var page = Page.Scan(handle.Window);
            handle.Screenshot(Path.Combine(_screenshotDir, "step1_initial.png"));

            // 3. Read initial value
            Assert.AreEqual("Hello World", page.GetByValue("Hello World").ReadText());

            // 4. Edit text field
            page.GetByValue("Hello World").Fill("Inspector Black Box");
            handle.Repaint();
            yield return null;

            page.Refresh();
            Assert.AreEqual("Inspector Black Box",
                page.GetByValue("Inspector Black Box").ReadText());

            // 5. Enable toggle group
            page.GetByType(ControlType.Toggle, 0).Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();
            handle.Screenshot(Path.Combine(_screenshotDir, "step2_group_enabled.png"));

            // 6. Inner slider is now accessible
            var slider = page.GetByValue("1.23");
            Assert.IsNotNull(slider);

            // 7. Close
            _driver.CloseAll();
        }

        // ── CloseInspector ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator CloseInspector_CleansUpGameObjectAndWindow()
        {
            var handle = _driver.OpenInspector<MyComponent>();
            yield return null;

            var window = handle.Window;
            var go = handle.GameObject;

            _driver.CloseInspector(handle);

            Assert.IsTrue(window == null, "Host window should be destroyed");
            Assert.IsTrue(go == null, "Temporary GameObject should be destroyed");
        }
    }
}
