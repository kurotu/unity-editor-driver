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
    /// Demonstrates the non-invasive UniEditWright API — MyWindow uses plain IMGUI.
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
        }

        private static ImguiPage DescribeMyWindow(EditorWindow window)
        {
            return ImguiPage.Describe(window, p =>
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
        public IEnumerator ReadDefaultFieldValues_ViaReflection()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            Assert.AreEqual("Hello World", handle.GetFieldValue<string>("myString"));
            Assert.AreEqual(true, handle.GetFieldValue<bool>("myBool"));
            Assert.AreEqual(1.23f, handle.GetFieldValue<float>("myFloat"), 0.001f);
        }

        [UnityTest]
        public IEnumerator SetFieldValue_ThenVerify()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            handle.SetFieldValue("myString", "Modified by test");
            handle.Repaint();
            yield return null;

            Assert.AreEqual("Modified by test", handle.GetFieldValue<string>("myString"));
        }

        [UnityTest]
        public IEnumerator ImguiPage_FindsControls_ByLabel()
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
        public IEnumerator ImguiPage_FindsSlider()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var page = DescribeMyWindow(handle.Window);

            var slider = page.GetByLabel("Slider");
            Assert.IsNotNull(slider);
            Assert.AreEqual(ControlType.Slider, slider.ControlType);

            var rect = slider.Rect;
            Assert.Greater(rect.width, 0);
        }

        [UnityTest]
        public IEnumerator ClickAt_DoesNotThrow()
        {
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            var pos = handle.Window.position;
            Assert.DoesNotThrow(() => handle.ClickAt(pos.width / 2, pos.height / 2));
        }

        [UnityTest]
        public IEnumerator FullWorkflow_OpenInteractScreenshot()
        {
            // 1. Open window
            var handle = _driver.OpenWindow<MyWindow>();
            yield return null;

            // 2. Take initial screenshot
            handle.Screenshot(Path.Combine(_screenshotDir, "step1_initial.png"));

            // 3. Change a value via reflection
            handle.SetFieldValue("myString", "E2E Test Value");
            handle.Repaint();
            yield return null;

            // 4. Verify the change
            Assert.AreEqual("E2E Test Value", handle.GetFieldValue<string>("myString"));

            // 5. Take final screenshot
            handle.Screenshot(Path.Combine(_screenshotDir, "step2_modified.png"));
            Assert.IsTrue(File.Exists(Path.Combine(_screenshotDir, "step2_modified.png")));

            // 6. Verify ImguiPage can describe the window
            var page = DescribeMyWindow(handle.Window);
            var textField = page.GetByLabel("Text Field");
            Assert.IsNotNull(textField);

            // 7. Close
            _driver.CloseAll();
        }
    }
}
