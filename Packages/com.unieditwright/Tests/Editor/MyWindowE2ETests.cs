using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    /// <summary>
    /// End-to-end integration test using MyWindow as the test target.
    /// Fully black-box: no reflection into MyWindow's fields.
    /// All verification is done through UI reads (ReadText, Screenshot).
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
            _screenshotDir = Path.Combine("UniEditWright_E2E_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_screenshotDir);
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Dispose();
            if (Directory.Exists(_screenshotDir))
                Directory.Delete(_screenshotDir, recursive: true);
        }

        private static Page DescribeMyWindow(EditorWindow window)
        {
            return Page.Describe(window, p =>
            {
                p.Label("Base Settings", EditorStyles.boldLabel);
                p.TextField("Text Field");
                p.BeginToggleGroup("Optional Settings");
                p.Toggle("Toggle");
                p.Slider("Slider", -3, 3);
                p.EndToggleGroup();
            });
        }

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

        [UnityTest]
        public IEnumerator ReadTextField_ReturnsDisplayedValue()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = DescribeMyWindow(handle.Window);
            var text = page.GetByLabel("Text Field").ReadText();

            Assert.AreEqual("Hello World", text);
        }

        [UnityTest]
        public IEnumerator FillTextField_ThenReadBack()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = DescribeMyWindow(handle.Window);
            var textField = page.GetByLabel("Text Field");

            textField.Fill("E2E Test Value");
            handle.Repaint();
            yield return null;

            var readBack = textField.ReadText();
            Assert.AreEqual("E2E Test Value", readBack);
        }

        [UnityTest]
        public IEnumerator ToggleGroup_ClickEnablesGroup()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = DescribeMyWindow(handle.Window);

            // Before toggling: group is disabled, so inner controls are unresponsive.
            // ReadText on Slider should return empty because the disabled field won't focus.
            var sliderTextBefore = page.GetByLabel("Slider").ReadText();
            Assert.AreEqual("", sliderTextBefore,
                "Slider should not be readable when toggle group is disabled");

            // Click the toggle group to enable it
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return null;

            // After toggling: group is enabled, controls respond to input.
            var sliderTextAfter = page.GetByLabel("Slider").ReadText();
            Assert.AreNotEqual("", sliderTextAfter,
                "Slider should be readable when toggle group is enabled");
        }

        [UnityTest]
        public IEnumerator Page_FindsControls_ByLabel()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = DescribeMyWindow(handle.Window);

            var textField = page.GetByLabel("Text Field");
            Assert.IsNotNull(textField);
            Assert.AreEqual("Text Field", textField.Label);
            Assert.AreEqual(ControlType.TextField, textField.ControlType);

            var rect = textField.Rect;
            Assert.Greater(rect.width, 0, "TextField rect should have positive width");
            Assert.Greater(rect.height, 0, "TextField rect should have positive height");
        }

        [UnityTest]
        public IEnumerator Page_FindsSlider()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = DescribeMyWindow(handle.Window);

            var slider = page.GetByLabel("Slider");
            Assert.IsNotNull(slider);
            Assert.AreEqual(ControlType.Slider, slider.ControlType);
            Assert.Greater(slider.Rect.width, 0);
        }

        [UnityTest]
        public IEnumerator FullWorkflow_BlackBox()
        {
            // 1. Open window
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = DescribeMyWindow(handle.Window);

            // 2. Take initial screenshot
            handle.Screenshot(Path.Combine(_screenshotDir, "step1_initial.png"));

            // 3. Read initial text field value
            var initialText = page.GetByLabel("Text Field").ReadText();
            Assert.AreEqual("Hello World", initialText);

            // 4. Type new text
            page.GetByLabel("Text Field").Fill("Black Box Test");
            handle.Repaint();
            yield return null;

            // 5. Verify via ReadText (no reflection)
            var modifiedText = page.GetByLabel("Text Field").ReadText();
            Assert.AreEqual("Black Box Test", modifiedText);

            // 6. Enable toggle group via UI click
            page.GetByLabel("Optional Settings").Toggle();
            handle.Repaint();
            yield return null;

            // 7. Take final screenshot
            handle.Screenshot(Path.Combine(_screenshotDir, "step2_modified.png"));
            Assert.IsTrue(File.Exists(Path.Combine(_screenshotDir, "step2_modified.png")));

            // 8. Close
            _driver.CloseAll();
        }

    }
}
