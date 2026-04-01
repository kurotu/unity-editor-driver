using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UniEditWright.Tests
{
    [TestFixture]
    public class WindowHandleTests
    {
        private EditorWindow _window;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _window = EditorWindow.GetWindow<MyWindow>();
            _window.Show();

            _tempDir = Path.Combine(Path.GetTempPath(), "UniEditWright_HandleTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
                _window.Close();

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public void Constructor_NullWindowThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new WindowHandle(null));
        }

        [Test]
        public void Window_ReturnsWrappedWindow()
        {
            var handle = new WindowHandle(_window);

            Assert.AreSame(_window, handle.Window);
        }

        [Test]
        public void GetFieldValue_ReadsPrivateField()
        {
            var handle = new WindowHandle(_window);

            // MyWindow has: string myString = "Hello World";
            var value = handle.GetFieldValue<string>("myString");

            Assert.AreEqual("Hello World", value);
        }

        [Test]
        public void SetFieldValue_WritesPrivateField()
        {
            var handle = new WindowHandle(_window);

            handle.SetFieldValue<string>("myString", "Changed");
            var value = handle.GetFieldValue<string>("myString");

            Assert.AreEqual("Changed", value);
        }

        [Test]
        public void GetFieldValue_NonExistentFieldThrows()
        {
            var handle = new WindowHandle(_window);

            Assert.Throws<MissingFieldException>(() => handle.GetFieldValue<string>("nonExistentField"));
        }

        [Test]
        public void Repaint_DoesNotThrow()
        {
            var handle = new WindowHandle(_window);

            Assert.DoesNotThrow(() => handle.Repaint());
        }

        [Test]
        public void ClickAt_DoesNotThrow()
        {
            var handle = new WindowHandle(_window);

            Assert.DoesNotThrow(() => handle.ClickAt(10f, 10f));
        }

        [Test]
        public void TypeText_DoesNotThrow()
        {
            var handle = new WindowHandle(_window);

            Assert.DoesNotThrow(() => handle.TypeText("hello"));
        }

        [Test]
        public void SendKey_DoesNotThrow()
        {
            var handle = new WindowHandle(_window);

            Assert.DoesNotThrow(() => handle.SendKey(KeyCode.Return));
        }

        [Test]
        public void Screenshot_CreatesFile()
        {
            var handle = new WindowHandle(_window);
            var filePath = Path.Combine(_tempDir, "handle_screenshot.png");

            handle.Screenshot(filePath);

            Assert.IsTrue(File.Exists(filePath), "Screenshot file should be created");
        }
    }
}
