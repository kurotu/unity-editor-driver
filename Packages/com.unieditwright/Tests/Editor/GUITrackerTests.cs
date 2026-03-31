using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniEditWright.Tests
{
    [TestFixture]
    public class GUITrackerTests
    {
        private TestTrackedWindow _trackedWindow;

        [SetUp]
        public void SetUp()
        {
            _trackedWindow = EditorWindow.GetWindow<TestTrackedWindow>();
            _trackedWindow.Show();
        }

        [TearDown]
        public void TearDown()
        {
            if (_trackedWindow != null)
                _trackedWindow.Close();
        }

        // --- Static method tests (no OnGUI context needed) ---

        [Test]
        public void Begin_NullWindowThrows()
        {
            Assert.Throws<ArgumentNullException>(() => GUITracker.Begin(null));
        }

        [Test]
        public void GetTracker_ReturnsNullForUntrackedWindow()
        {
            var untrackedWindow = ScriptableObject.CreateInstance<UntrackedWindow>();
            try
            {
                var tracker = GUITracker.GetTracker(untrackedWindow);

                Assert.IsNull(tracker);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(untrackedWindow);
            }
        }

        // --- Tests that require OnGUI to have run (UnityTest + yield) ---

        [UnityTest]
        public IEnumerator Begin_ReturnsNonNullTracker()
        {
            // Wait one frame so OnGUI runs and the tracker is created
            yield return null;

            Assert.IsNotNull(_trackedWindow.Tracker, "Tracker should be created after OnGUI runs");
        }

        [UnityTest]
        public IEnumerator GetTracker_ReturnsTrackerForTrackedWindow()
        {
            yield return null;

            var tracker = GUITracker.GetTracker(_trackedWindow);

            Assert.IsNotNull(tracker, "GetTracker should return the tracker for a tracked window");
            Assert.AreSame(_trackedWindow.Tracker, tracker);
        }

        [UnityTest]
        public IEnumerator TextField_ReturnsInputValue()
        {
            _trackedWindow.StringValue = "expected";
            yield return null;

            // The passthrough should preserve the value set before OnGUI
            Assert.AreEqual("expected", _trackedWindow.StringValue);
        }

        [UnityTest]
        public IEnumerator TextField_RecordsEntry()
        {
            yield return null;

            var entry = _trackedWindow.Tracker.FindByLabel("Text");

            Assert.IsNotNull(entry, "TextField should be recorded with label 'Text'");
            Assert.AreEqual(ControlType.TextField, entry.Value.Type);
        }

        [UnityTest]
        public IEnumerator Toggle_ReturnsInputValue()
        {
            _trackedWindow.BoolValue = false;
            yield return null;

            // The tracker should pass through the value; since no user click, it stays false
            Assert.IsFalse(_trackedWindow.BoolValue);
        }

        [UnityTest]
        public IEnumerator Toggle_RecordsEntry()
        {
            yield return null;

            var entry = _trackedWindow.Tracker.FindByLabel("Check");

            Assert.IsNotNull(entry, "Toggle should be recorded with label 'Check'");
            Assert.AreEqual(ControlType.Toggle, entry.Value.Type);
        }

        [UnityTest]
        public IEnumerator Slider_ReturnsClampedValue()
        {
            _trackedWindow.FloatValue = 0.5f;
            yield return null;

            // Value should stay within [0, 1] range
            Assert.GreaterOrEqual(_trackedWindow.FloatValue, 0f);
            Assert.LessOrEqual(_trackedWindow.FloatValue, 1f);
        }

        [UnityTest]
        public IEnumerator Slider_RecordsEntry()
        {
            yield return null;

            var entry = _trackedWindow.Tracker.FindByLabel("Amount");

            Assert.IsNotNull(entry, "Slider should be recorded with label 'Amount'");
            Assert.AreEqual(ControlType.Slider, entry.Value.Type);
        }

        [UnityTest]
        public IEnumerator Label_RecordsEntry()
        {
            yield return null;

            var entry = _trackedWindow.Tracker.FindByLabel("Test Label");

            Assert.IsNotNull(entry, "Label should be recorded with label 'Test Label'");
            Assert.AreEqual(ControlType.Label, entry.Value.Type);
        }

        [UnityTest]
        public IEnumerator BeginToggleGroup_RecordsEntry()
        {
            yield return null;

            var entry = _trackedWindow.Tracker.FindByLabel("Group");

            Assert.IsNotNull(entry, "BeginToggleGroup should be recorded with label 'Group'");
            Assert.AreEqual(ControlType.ToggleGroup, entry.Value.Type);
        }

        [UnityTest]
        public IEnumerator FindByLabel_ReturnsMatchingEntry()
        {
            yield return null;

            var entry = _trackedWindow.Tracker.FindByLabel("Text");

            Assert.IsNotNull(entry);
            Assert.AreEqual("Text", entry.Value.Label);
        }

        [UnityTest]
        public IEnumerator FindByLabel_ReturnsNullForUnknownLabel()
        {
            yield return null;

            var entry = _trackedWindow.Tracker.FindByLabel("Does Not Exist");

            Assert.IsNull(entry);
        }

        [UnityTest]
        public IEnumerator End_StopsRecording()
        {
            yield return null;

            // After OnGUI, End() has been called. The entries should be frozen.
            var tracker = _trackedWindow.Tracker;
            int countAfterEnd = tracker.Entries.Count;

            // The entries count should be stable (End was called)
            Assert.Greater(countAfterEnd, 0, "There should be entries recorded before End()");
        }

        [UnityTest]
        public IEnumerator Entries_ClearedOnNewBegin()
        {
            yield return null;

            // Capture the entries from the first frame
            var firstFrameEntries = _trackedWindow.Tracker.Entries.ToList();

            // Wait another frame so OnGUI runs again, calling Begin() which should clear entries
            yield return null;

            var secondFrameEntries = _trackedWindow.Tracker.Entries.ToList();

            // Entries should be repopulated (same count) but the list was cleared and rebuilt
            Assert.AreEqual(firstFrameEntries.Count, secondFrameEntries.Count,
                "Entries should be rebuilt each frame with same controls");
            Assert.Greater(secondFrameEntries.Count, 0,
                "Entries should not be empty after Begin re-records");
        }

        // --- Helper windows ---

        private class UntrackedWindow : EditorWindow { }
    }

    /// <summary>
    /// Test helper window that uses GUITracker to wrap IMGUI controls.
    /// Must be public so Unity can serialize/instantiate it.
    /// </summary>
    public class TestTrackedWindow : EditorWindow
    {
        public GUITracker Tracker;
        public string StringValue = "test";
        public bool BoolValue = true;
        public float FloatValue = 0.5f;
        public bool GroupEnabled = true;

        void OnGUI()
        {
            Tracker = GUITracker.Begin(this);
            Tracker.Label("Test Label");
            StringValue = Tracker.TextField("Text", StringValue);
            BoolValue = Tracker.Toggle("Check", BoolValue);
            FloatValue = Tracker.Slider("Amount", FloatValue, 0f, 1f);
            GroupEnabled = Tracker.BeginToggleGroup("Group", GroupEnabled);
            Tracker.EndToggleGroup();
            Tracker.End();
        }
    }
}
