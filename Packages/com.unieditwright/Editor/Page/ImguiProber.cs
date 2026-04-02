using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Auto-discovers IMGUI controls by probing an EditorWindow via SendEvent.
    /// Each probe sends a synthetic mouse click and checks which control captures
    /// keyboard focus. Text field values are read via the clipboard.
    /// The window state is saved before probing and restored afterward.
    /// </summary>
    internal static class ImguiProber
    {
        private struct ProbeHit
        {
            public float Y;
            public int KeyboardControl;
            public bool EventUsed;
        }

        /// <summary>
        /// Probes the window and returns a list of auto-discovered controls.
        /// Controls are classified as <see cref="ControlType.TextField"/> (editable text)
        /// or <see cref="ControlType.Toggle"/> (clickable, no text).
        /// </summary>
        public static List<ControlInfo> Probe(EditorWindow window)
        {
            float contentY = GetContentYOffset(window);
            float windowW = window.position.width;
            float windowH = window.position.height + contentY;

            // Save window state so probing is non-destructive
            var snapshot = SaveState(window);

            try
            {
                // Phase 1: vertical scan to find interactive regions
                var hits = ScanVertical(window, contentY, windowH, windowW, snapshot);

                // Phase 2: merge hits into control regions
                var regions = MergeRegions(hits, windowW, contentY);

                // Phase 3: read text values for field-type controls
                ReadTextValues(window, regions, snapshot);

                return regions;
            }
            finally
            {
                RestoreState(window, snapshot);
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;
            }
        }

        // ── Phase 1: Vertical scan ─────────────────────────────────

        private static List<ProbeHit> ScanVertical(
            EditorWindow window, float startY, float endY, float windowW,
            (FieldInfo field, object value)[] snapshot)
        {
            // Step size: half of singleLineHeight to guarantee hitting each control
            float step = Mathf.Max(EditorGUIUtility.singleLineHeight / 2f, 4f);
            float probeX = windowW * 0.75f;

            var hits = new List<ProbeHit>();

            for (float y = startY; y < endY; y += step)
            {
                // Restore state before each probe so toggle clicks don't affect
                // subsequent probes (each probe sees the original window state).
                RestoreState(window, snapshot);

                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;

                var md = new Event { type = EventType.MouseDown };
                md.mousePosition = new Vector2(probeX, y);
                md.button = 0;
                bool used = window.SendEvent(md);

                var mu = new Event { type = EventType.MouseUp };
                mu.mousePosition = new Vector2(probeX, y);
                mu.button = 0;
                window.SendEvent(mu);

                hits.Add(new ProbeHit
                {
                    Y = y,
                    KeyboardControl = GUIUtility.keyboardControl,
                    EventUsed = used
                });
            }

            return hits;
        }

        // ── Phase 2: Merge into regions ─────────────────────────────

        private static List<ControlInfo> MergeRegions(
            List<ProbeHit> hits, float windowW, float contentY)
        {
            var controls = new List<ControlInfo>();
            float lineHeight = EditorGUIUtility.singleLineHeight;

            int i = 0;
            while (i < hits.Count)
            {
                var hit = hits[i];

                // Skip non-interactive positions
                if (hit.KeyboardControl == 0 && !hit.EventUsed)
                {
                    i++;
                    continue;
                }

                // Found an interactive position — find the region extent
                int kbCtrl = hit.KeyboardControl;
                bool used = hit.EventUsed;
                float startY = hit.Y;
                float endY = hit.Y;

                int j = i + 1;
                while (j < hits.Count &&
                       hits[j].KeyboardControl == kbCtrl &&
                       hits[j].EventUsed == used)
                {
                    endY = hits[j].Y;
                    j++;
                }

                // Skip tiny regions (< half a control height) — likely noise
                if (endY - startY < lineHeight / 3f)
                {
                    i = j;
                    continue;
                }

                // Determine type
                ControlType type;
                if (kbCtrl > 0)
                {
                    // Keyboard-capturing: either text field or toggle.
                    // We'll distinguish in Phase 3 by trying ReadText.
                    type = ControlType.TextField;
                }
                else if (used)
                {
                    type = ControlType.Button;
                }
                else
                {
                    i = j;
                    continue;
                }

                // Build rect — clamp to singleLineHeight if the region is taller
                float rectY = startY;
                float height = Mathf.Min(endY - startY + lineHeight / 2f, lineHeight);
                var rect = new Rect(0, rectY, windowW, height);

                controls.Add(new ControlInfo(type, rect));
                i = j;
            }

            return controls;
        }

        // ── Phase 3: Read text values ──────────────────────────────

        private static void ReadTextValues(
            EditorWindow window, List<ControlInfo> controls,
            (FieldInfo field, object value)[] snapshot)
        {
            foreach (var control in controls)
            {
                if (control.Type != ControlType.TextField) continue;

                RestoreState(window, snapshot);
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;

                // Click the control's field area
                float probeX = control.Rect.x + control.Rect.width * 0.75f;
                float probeY = control.Rect.y + control.Rect.height / 2f;

                var md = new Event { type = EventType.MouseDown };
                md.mousePosition = new Vector2(probeX, probeY);
                md.button = 0;
                window.SendEvent(md);

                var mu = new Event { type = EventType.MouseUp };
                mu.mousePosition = new Vector2(probeX, probeY);
                mu.button = 0;
                window.SendEvent(mu);

                if (GUIUtility.keyboardControl == 0)
                {
                    // Couldn't focus — might be a clickable, not a text field
                    control.Type = ControlType.Toggle;
                    control.Value = null;
                    continue;
                }

                // SelectAll + Copy via clipboard
                string savedClip = GUIUtility.systemCopyBuffer;
                GUIUtility.systemCopyBuffer = "";

                window.SendEvent(new Event
                    { type = EventType.ValidateCommand, commandName = "SelectAll" });
                window.SendEvent(new Event
                    { type = EventType.ExecuteCommand, commandName = "SelectAll" });
                window.SendEvent(new Event
                    { type = EventType.ValidateCommand, commandName = "Copy" });
                window.SendEvent(new Event
                    { type = EventType.ExecuteCommand, commandName = "Copy" });

                string text = GUIUtility.systemCopyBuffer;
                GUIUtility.systemCopyBuffer = savedClip;

                if (string.IsNullOrEmpty(text))
                {
                    // No text → this is a toggle/checkbox, not a text field
                    control.Type = ControlType.Toggle;
                    control.Value = null;
                }
                else
                {
                    control.Value = text;
                }
            }
        }

        // ── State save/restore ──────────────────────────────────────

        private static (FieldInfo field, object value)[] SaveState(EditorWindow window)
        {
            var type = window.GetType();
            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            var snapshot = new (FieldInfo, object)[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                snapshot[i] = (fields[i], fields[i].GetValue(window));

            return snapshot;
        }

        private static void RestoreState(
            EditorWindow window, (FieldInfo field, object value)[] snapshot)
        {
            foreach (var (field, value) in snapshot)
                field.SetValue(window, value);
        }

        // ── Tab-bar offset (shared with ImguiLayoutResolver) ────────

        private static float GetContentYOffset(EditorWindow window)
        {
            try
            {
                var parentField = typeof(EditorWindow).GetField(
                    "m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
                if (parentField == null) return 0f;

                var parent = parentField.GetValue(window);
                if (parent == null) return 0f;

                var borderProp = parent.GetType().GetProperty(
                    "borderSize",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (borderProp == null) return 0f;

                var border = borderProp.GetValue(parent) as RectOffset;
                return border?.top ?? 0f;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
