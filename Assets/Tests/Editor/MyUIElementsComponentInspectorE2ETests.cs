using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    /// <summary>
    /// End-to-end integration test using MyUIElementsComponent's UIElements inspector.
    /// Mirrors <see cref="MyUIElementsWindowE2ETests"/> to verify the inspector testing path
    /// works identically to the EditorWindow path for UIElements.
    ///
    /// The InspectorHostWindow detects <see cref="UnityEditor.Editor.CreateInspectorGUI"/>
    /// and hosts the UIElements visual tree automatically.
    ///
    /// MyUIElementsComponent initial state:
    ///   - TextField "Text Field": "Hello World"
    ///   - Toggle "Optional Settings": disabled — inner controls are inactive
    ///   - Toggle "Toggle" / Slider "Slider" inside group: NOT discoverable until group is enabled
    /// </summary>
    [TestFixture]
    public class MyUIElementsComponentInspectorE2ETests
    {
        private EditorDriver _driver;
        private string _screenshotDir;

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
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
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
            var handle = _driver.OpenInspector<MyUIElementsComponent, MyUIElementsComponentEditor>();
            yield return null;

            Assert.IsInstanceOf<MyUIElementsComponentEditor>(handle.Editor);
        }

        // ── Screenshot ──────────────────────────────────────────────

        [UnityTest]
        public IEnumerator OpenInspector_TakeScreenshot_WindowIsVisible()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return null;

            var path = Path.Combine(_screenshotDir, "uielements_inspector_initial.png");
            handle.Screenshot(path);
            Assert.IsTrue(File.Exists(path), "Screenshot should be saved");
        }

        // ── Scan — Label-based lookup ───────────────────────────────

        [UnityTest]
        public IEnumerator Scan_FindTextFieldByLabel_ReturnsLocator()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return null;

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
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var locator = page.GetByValue("Hello World");

            Assert.AreEqual(ControlType.TextField, locator.ControlType);
        }

        [UnityTest]
        public IEnumerator Scan_ReadTextField_ReturnsDisplayedValue()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            var page = Page.Scan(handle.Window);
            var text = page.GetByLabel("Text Field").ReadText();

            Assert.AreEqual("Hello World", text);
        }

        // ── Fill ────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_FillTextField_ThenReadBack()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            var page = Page.Scan(handle.Window);
            page.GetByLabel("Text Field").Fill("Inspector UIElements Test");
            handle.Repaint();
            yield return null;

            page.Refresh();
            var readBack = page.GetByValue("Inspector UIElements Test").ReadText();
            Assert.AreEqual("Inspector UIElements Test", readBack);
        }

        // ── Toggle group ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_InitialState_DisabledGroupControlsNotDiscoverable()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);

            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByLabel("Slider"),
                "Slider inside disabled group should not be discoverable");
        }

        [UnityTest]
        public IEnumerator Scan_EnableToggleGroup_RevealedInnerControls()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return null;

            var page = Page.Scan(handle.Window);
            int countBefore = page.Controls.Count;

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
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
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

        // ── Full workflow ───────────────────────────────────────────

        [UnityTest]
        public IEnumerator FullWorkflow_BlackBox()
        {
            // 1. Open inspector
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return WaitForBindings();

            // 2. Scan — labels and values available
            var page = Page.Scan(handle.Window);
            handle.Screenshot(Path.Combine(_screenshotDir, "step1_initial.png"));

            // 3. Verify initial value
            Assert.AreEqual("Hello World", page.GetByValue("Hello World").ReadText());

            // 4. Edit text field
            page.GetByLabel("Text Field").Fill("Inspector UIElements Black Box");
            handle.Repaint();
            yield return WaitForBindings();

            page.Refresh();
            Assert.AreEqual("Inspector UIElements Black Box",
                page.GetByValue("Inspector UIElements Black Box").ReadText());

            // 5. Enable toggle group
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return null;

            page.Refresh();
            handle.Screenshot(Path.Combine(_screenshotDir, "step2_group_enabled.png"));

            // 6. Inner slider is now accessible
            var slider = page.GetByLabel("Slider");
            Assert.IsNotNull(slider);

            // 7. Close
            _driver.CloseAll();
        }

        // ── CloseInspector ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator CloseInspector_CleansUpGameObjectAndWindow()
        {
            var handle = _driver.OpenInspector<MyUIElementsComponent>();
            yield return null;

            var window = handle.Window;
            var go = handle.GameObject;

            _driver.CloseInspector(handle);

            Assert.IsTrue(window == null, "Host window should be destroyed");
            Assert.IsTrue(go == null, "Temporary GameObject should be destroyed");
        }
    }
}
