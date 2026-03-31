using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Provides tracked wrappers for IMGUI controls.
    /// Users add this to their EditorWindow.OnGUI() for precise control discovery.
    /// Usage:
    ///   var t = GUITracker.Begin(this);
    ///   myString = t.TextField("Label", myString);
    ///   t.End();
    /// </summary>
    public class GUITracker
    {
        private static readonly Dictionary<EditorWindow, GUITracker> _trackers =
            new Dictionary<EditorWindow, GUITracker>();

        private readonly EditorWindow _window;
        private readonly List<ControlEntry> _entries = new List<ControlEntry>();
        private bool _recording;

        private GUITracker(EditorWindow window)
        {
            _window = window;
        }

        public IReadOnlyList<ControlEntry> Entries => _entries;

        public static GUITracker Begin(EditorWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            if (!_trackers.TryGetValue(window, out var tracker))
            {
                tracker = new GUITracker(window);
                _trackers[window] = tracker;
            }

            tracker._entries.Clear();
            tracker._recording = true;
            return tracker;
        }

        public static GUITracker GetTracker(EditorWindow window)
        {
            if (window == null) return null;
            _trackers.TryGetValue(window, out var tracker);
            return tracker;
        }

        public void End()
        {
            _recording = false;
        }

        public void Label(string text, GUIStyle style)
        {
            if (style != null)
                GUILayout.Label(text, style);
            else
                GUILayout.Label(text);
            RecordIfRepainting(text, ControlType.Label);
        }

        public void Label(string text)
        {
            Label(text, null);
        }

        public string TextField(string label, string value)
        {
            var result = EditorGUILayout.TextField(label, value);
            RecordIfRepainting(label, ControlType.TextField);
            return result;
        }

        public bool Toggle(string label, bool value)
        {
            var result = EditorGUILayout.Toggle(label, value);
            RecordIfRepainting(label, ControlType.Toggle);
            return result;
        }

        public float Slider(string label, float value, float leftValue, float rightValue)
        {
            var result = EditorGUILayout.Slider(label, value, leftValue, rightValue);
            RecordIfRepainting(label, ControlType.Slider);
            return result;
        }

        public bool BeginToggleGroup(string label, bool toggle)
        {
            var result = EditorGUILayout.BeginToggleGroup(label, toggle);
            // Cannot use GetLastRect after beginning a group — record directly
            if (_recording)
            {
                _entries.Add(new ControlEntry(label, Rect.zero, ControlType.ToggleGroup));
            }
            return result;
        }

        public void EndToggleGroup()
        {
            EditorGUILayout.EndToggleGroup();
        }

        public ControlEntry? FindByLabel(string label)
        {
            foreach (var entry in _entries)
            {
                if (entry.Label == label)
                    return entry;
            }
            return null;
        }

        private void RecordIfRepainting(string label, ControlType type)
        {
            if (!_recording) return;
            var rect = GUILayoutUtility.GetLastRect();
            _entries.Add(new ControlEntry(label, rect, type));
        }
    }
}
