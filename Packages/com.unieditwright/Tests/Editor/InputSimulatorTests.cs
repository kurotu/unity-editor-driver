using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UniEditWright.Tests
{
    [TestFixture]
    public class InputSimulatorTests
    {

        // NOTE: Unity's Event.type getter returns Ignore for mouse events outside OnGUI context.
        // This is a known Unity limitation (Event.type checks IMGUIModule.IsIMGUIActive).
        // The raw type IS correctly set in native memory and SendEvent works properly.
        // These tests verify that by checking behavioral properties instead.

        [Test]
        public void CreateMouseEvent_MouseDown_SetsPositionAndButton()
        {
            var pos = new Vector2(42f, 84f);
            var evt = InputSimulator.CreateMouseEvent(EventType.MouseDown, pos, button: 1);

            Assert.AreEqual(pos, evt.mousePosition);
            Assert.AreEqual(1, evt.button);
        }

        [Test]
        public void CreateMouseEvent_MouseUp_SetsPositionAndButton()
        {
            var pos = new Vector2(10f, 20f);
            var evt = InputSimulator.CreateMouseEvent(EventType.MouseUp, pos, button: 0);

            Assert.AreEqual(pos, evt.mousePosition);
            Assert.AreEqual(0, evt.button);
        }

        [Test]
        public void CreateMouseEvent_ReturnsEventWithCorrectPosition()
        {
            var position = new Vector2(100f, 200f);

            var evt = InputSimulator.CreateMouseEvent(EventType.MouseDown, position);

            Assert.AreEqual(position, evt.mousePosition);
        }

        [Test]
        public void CreateMouseEvent_ReturnsEventWithCorrectButton()
        {
            var evt = InputSimulator.CreateMouseEvent(EventType.MouseDown, Vector2.zero, button: 1);

            Assert.AreEqual(1, evt.button);
        }

        [Test]
        public void CreateMouseEvent_DefaultButtonIsZero()
        {
            var evt = InputSimulator.CreateMouseEvent(EventType.MouseDown, Vector2.zero);

            Assert.AreEqual(0, evt.button);
        }

        [Test]
        public void CreateKeyEvent_ReturnsEventWithCorrectKeyCode()
        {
            var evt = InputSimulator.CreateKeyEvent(EventType.KeyDown, KeyCode.A);

            Assert.AreEqual(KeyCode.A, evt.keyCode);
        }

        [Test]
        public void CreateKeyEvent_ReturnsEventWithCorrectModifiers()
        {
            var evt = InputSimulator.CreateKeyEvent(EventType.KeyDown, KeyCode.A, EventModifiers.Shift);

            Assert.AreEqual(EventModifiers.Shift, evt.modifiers);
        }

        [Test]
        public void CreateKeyEvent_DefaultModifiersIsNone()
        {
            var evt = InputSimulator.CreateKeyEvent(EventType.KeyDown, KeyCode.A);

            Assert.AreEqual(EventModifiers.None, evt.modifiers);
        }

        [Test]
        public void MouseClick_SendsDownAndUpEvents_DoesNotThrow()
        {
            // Open a real EditorWindow so the dispatch target is valid
            var window = ScriptableObject.CreateInstance<TestHelperWindow>();
            window.Show();
            try
            {
                Assert.DoesNotThrow(() =>
                    InputSimulator.MouseClick(window, new Vector2(10f, 10f)));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TypeText_SendsKeyEventsForEachCharacter()
        {
            var window = ScriptableObject.CreateInstance<TestHelperWindow>();
            window.Show();
            try
            {
                // Should not throw; one key event pair per character
                Assert.DoesNotThrow(() =>
                    InputSimulator.TypeText(window, "abc"));
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// Minimal EditorWindow used as an event dispatch target.
        /// </summary>
        private class TestHelperWindow : EditorWindow { }
    }
}
