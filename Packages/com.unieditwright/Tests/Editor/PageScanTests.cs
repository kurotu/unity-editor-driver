using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    /// <summary>
    /// Tests for <see cref="Page.Scan"/> auto-discovery.
    /// Uses MyWindow as a black-box target — no reflection, no manual descriptors.
    /// </summary>
    [TestFixture]
    public class PageScanTests
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

        [UnityTest]
        public IEnumerator Scan_DiscoversTextField()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            // MyWindow has a TextField with "Hello World"
            var locator = page.GetByValue("Hello World");
            Assert.IsNotNull(locator);
            Assert.AreEqual(ControlType.TextField, locator.ControlType);
        }

        [UnityTest]
        public IEnumerator Scan_DiscoversToggle()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            // There should be at least one toggle-type control
            // (the "Optional Settings" BeginToggleGroup checkbox)
            var locator = page.GetByType(ControlType.Toggle, 0);
            Assert.IsNotNull(locator);
        }

        [UnityTest]
        public IEnumerator Scan_ReadText_ReturnsFieldValue()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var text = page.GetByValue("Hello World").ReadText();

            Assert.AreEqual("Hello World", text);
        }

        [UnityTest]
        public IEnumerator Scan_FillTextField_ThenReadBack()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var textField = page.GetByValue("Hello World");

            textField.Fill("Scanned Fill");
            handle.Repaint();
            yield return null;

            // After fill, re-scan to see the updated value
            page.Refresh();
            var updated = page.GetByValue("Scanned Fill");
            Assert.IsNotNull(updated);
            Assert.AreEqual("Scanned Fill", updated.ReadText());
        }

        [UnityTest]
        public IEnumerator Scan_Refresh_DetectsDynamicChanges()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            // Resize the window so all controls (including those inside the
            // disabled BeginToggleGroup) are fully visible and clickable.
            handle.Window.position = new UnityEngine.Rect(100, 100, 520, 300);
            yield return null;

            var page = Page.Scan(handle.Window);

            int initialCount = page.Controls.Count;

            // Enable the toggle group — this should make inner controls interactive.
            // The BeginToggleGroup header is detected at index 0 with a very small
            // rect whose center falls outside the actual clickable area.  The second
            // Toggle (index 1) overlaps the real checkbox region of BeginToggleGroup
            // and clicking it reliably flips groupEnabled.
            var toggle = page.GetByType(ControlType.Toggle, 1);
            toggle.Toggle();
            handle.Repaint();
            yield return null;

            // Re-scan
            page.Refresh();

            // After enabling group, more controls should be discoverable
            Assert.Greater(page.Controls.Count, initialCount,
                "Enabling toggle group should reveal inner controls");
        }

        [UnityTest]
        public IEnumerator Scan_GetByType_IndexAccess()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            // Should have at least one TextField
            var field0 = page.GetByType(ControlType.TextField, 0);
            Assert.IsNotNull(field0);
            Assert.AreEqual(ControlType.TextField, field0.ControlType);
        }

        [UnityTest]
        public IEnumerator Scan_NonDestructive_StatePreserved()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            // Read the text field value before scan
            var pageBefore = Page.Describe(handle.Window, p =>
            {
                p.Label("Base Settings", EditorStyles.boldLabel);
                p.TextField("Text Field");
                p.ObjectField("Material");
                p.ObjectField("Texture");
                p.BeginToggleGroup("Optional Settings");
                p.Toggle("Toggle");
                p.Slider("Slider", -3, 3);
                p.EndToggleGroup();
            });
            var valueBefore = pageBefore.GetByLabel("Text Field").ReadText();

            // Perform a scan (probing sends synthetic events)
            Page.Scan(handle.Window);

            // Read the text field value after scan
            handle.Repaint();
            yield return null;
            pageBefore.Refresh();
            var valueAfter = pageBefore.GetByLabel("Text Field").ReadText();

            Assert.AreEqual(valueBefore, valueAfter,
                "Scan should not alter the window state");
        }

        [UnityTest]
        public IEnumerator Scan_GetByType_ThrowsOnOutOfRange()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByType(ControlType.TextField, 999));
        }

        [UnityTest]
        public IEnumerator Scan_GetByValue_ThrowsOnNotFound()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            Assert.Throws<System.InvalidOperationException>(() =>
                page.GetByValue("NonExistentValue12345"));
        }

        [UnityTest]
        public IEnumerator Scan_ControlsProperty_ReturnsDiscoveredList()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            Assert.IsNotNull(page.Controls);
            Assert.Greater(page.Controls.Count, 0,
                "Should discover at least one control");
        }

        // ── ObjectField ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Scan_DiscoversObjectField()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            // MyWindow has Material and Texture ObjectFields
            var locator = page.GetByType(ControlType.ObjectField, 0);
            Assert.IsNotNull(locator);
            Assert.AreEqual(ControlType.ObjectField, locator.ControlType);
        }

        [UnityTest]
        public IEnumerator Scan_DiscoversMultipleObjectFields()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);

            // Should find both Material and Texture ObjectFields
            var first = page.GetByType(ControlType.ObjectField, 0);
            var second = page.GetByType(ControlType.ObjectField, 1);
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
        }

        [UnityTest]
        public IEnumerator Scan_ObjectField_HasValidRect()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = Page.Scan(handle.Window);
            var locator = page.GetByType(ControlType.ObjectField, 0);

            Assert.Greater(locator.Rect.width, 0, "ObjectField should have positive width");
            Assert.Greater(locator.Rect.height, 0, "ObjectField should have positive height");
        }
    }
}
