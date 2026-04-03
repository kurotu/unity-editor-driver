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

        // Cached reflection access to GUIUtility.s_LastControlID.
        // Resetting this before each probe gives every probe the same starting ID,
        // so the same control always gets the same keyboardControl value and MergeRegions
        // can group hits reliably.
        private static FieldInfo _lastControlIdField;
        private static bool _lastControlIdSearched;

        private static void ResetControlIdCounter()
        {
            if (!_lastControlIdSearched)
            {
                _lastControlIdSearched = true;
                _lastControlIdField = typeof(GUIUtility).GetField(
                    "s_LastControlID",
                    BindingFlags.Static | BindingFlags.NonPublic);
            }
            // Reset to a large value far from any ID that existing IMGUI state
            // (hotControl, keyboardControl, TextEditor focus, etc.) might reference.
            // Resetting to 0 would collide with small existing IDs and cause phantom
            // controls when a text field has keyboard focus between probes.
            _lastControlIdField?.SetValue(null, 1_000_000);
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

            // Probe at the float-field zone: last fieldWidth/2 px from the right.
            // This reliably hits both TextField input areas AND Slider float fields,
            // while staying clear of the slider track in the middle.
            float probeX = windowW - EditorGUIUtility.fieldWidth / 2f;

            var snapshot = SaveState(window);

            try
            {
                var hits = ScanVertical(window, contentY, windowH, probeX, snapshot);
                var regions = MergeRegions(hits, windowW);
                ReadTextValues(window, regions, probeX, snapshot);
                DetectObjectFields(window, contentY, windowH, windowW, regions, snapshot);
                return regions;
            }
            finally
            {
                RestoreState(window, snapshot);
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;
            }
        }

        /// <summary>
        /// Clears the IMGUI RecycledEditor's controlID.
        /// After Fill/ReadText, EditorGUI.s_RecycledEditor retains a stale controlID
        /// that matches the previously focused TextField's IMGUI ID.  On the next probe,
        /// the TextField sees the matching ID and reclaims keyboard focus even though the
        /// mouse is not inside its rect — producing phantom kbCtrl captures at unrelated
        /// y-positions (e.g. y=21 where only a Label exists).  Setting controlID=0 breaks
        /// the match and prevents the phantom.
        /// </summary>
        private static void ClearRecycledEditorState()
        {
            try
            {
                var field = typeof(EditorGUI).GetField(
                    "s_RecycledEditor",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var editor = field?.GetValue(null) as TextEditor;
                if (editor == null) return;

                // Use direct assignment — TextEditor.controlID is a public field/property.
                // Reflection-based GetField("controlID") can silently return null in some
                // Unity versions (when the backing member is a property, not a bare field),
                // leaving the stale ID intact and re-enabling phantom focus reclaims.
                editor.controlID = 0;
            }
            catch { }
        }

        // ── Phase 1: Vertical scan ─────────────────────────────────

        private static List<ProbeHit> ScanVertical(
            EditorWindow window, float startY, float endY, float probeX,
            FieldSnapshot[] snapshot)
        {
            // Step ≤ singleLineHeight/2 so every control gets at least one probe.
            float step = Mathf.Max(EditorGUIUtility.singleLineHeight / 2f, 4f);

            var hits = new List<ProbeHit>();

            for (float y = startY; y < endY; y += step)
            {
                RestoreState(window, snapshot);
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;

                // Clear stale RecycledEditor state before each probe event.
                // After Fill/ReadText, EditorGUI.s_RecycledEditor retains a stale
                // controlID that can cause phantom focus captures when the ID counter
                // is reset. Clearing it here prevents phantom Toggle/TextField hits
                // at unexpected y-positions (e.g. y=21 where only a Label exists).
                ClearRecycledEditorState();

                // Reset the IMGUI control-ID counter so every probe produces the
                // same IDs for the same controls, enabling reliable grouping.
                ResetControlIdCounter();

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

        private static List<ControlInfo> MergeRegions(List<ProbeHit> hits, float windowW)
        {
            var controls = new List<ControlInfo>();
            float lineHeight = EditorGUIUtility.singleLineHeight;

            int i = 0;
            while (i < hits.Count)
            {
                var hit = hits[i];

                if (hit.KeyboardControl == 0 && !hit.EventUsed)
                {
                    i++;
                    continue;
                }

                int kbCtrl = hit.KeyboardControl;
                bool used = hit.EventUsed;
                float startY = hit.Y;
                float endY = hit.Y;

                // Extend region while consecutive hits share the same kbCtrl+used pair.
                // Because the ID counter is reset before each probe, the same control
                // reliably yields the same kbCtrl value on every probe.
                int j = i + 1;
                while (j < hits.Count &&
                       hits[j].KeyboardControl == kbCtrl &&
                       hits[j].EventUsed == used)
                {
                    endY = hits[j].Y;
                    j++;
                }

                ControlType type;
                if (kbCtrl > 0 && used)
                    // Real interactive control: event was consumed AND keyboard focus was
                    // captured.  Requires BOTH conditions because stale keyboardControl
                    // values can persist across SendEvent calls (phantom kbCtrl), but
                    // those phantom hits always have EventUsed=false.
                    type = ControlType.TextField;   // distinguished in Phase 3
                else if (kbCtrl == 0 && used)
                    type = ControlType.Button;
                else
                {
                    // kbCtrl > 0 but event NOT used → phantom from stale focus state.
                    // kbCtrl == 0 and not used → empty space.  Either way: skip.
                    i = j;
                    continue;
                }

                // Snap region height to at most one singleLineHeight
                float height = Mathf.Min(endY - startY + lineHeight / 2f, lineHeight);
                controls.Add(new ControlInfo(type, new Rect(0, startY, windowW, height)));
                i = j;
            }

            return controls;
        }

        // ── Phase 3: Read text values ──────────────────────────────

        private static void ReadTextValues(
            EditorWindow window, List<ControlInfo> controls,
            float probeX, FieldSnapshot[] snapshot)
        {
            foreach (var control in controls)
            {
                if (control.Type != ControlType.TextField) continue;

                RestoreState(window, snapshot);
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;
                ClearRecycledEditorState();
                ResetControlIdCounter();

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
                    // No keyboard capture → not a focusable text control
                    control.Type = ControlType.Toggle;
                    control.Value = null;
                    continue;
                }

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
                    control.Type = ControlType.Toggle;
                    control.Value = null;
                }
                else
                {
                    control.Value = text;
                }
            }

        }

        // ── Phase 4: Detect ObjectField via label-area scan ────────

        /// <summary>
        /// ObjectField with a null value does not consume MouseDown events in the
        /// field area, making it invisible to the primary scan at probeX.
        /// However, clicking in the label area (x ≈ 20) still sets keyboard focus
        /// to a unique control ID for each ObjectField.
        /// By scanning at a label-area X and looking for kbCtrl transitions that
        /// do not overlap with any already-detected control, we can identify
        /// ObjectField positions.
        /// </summary>
        private static void DetectObjectFields(
            EditorWindow window, float startY, float endY, float windowW,
            List<ControlInfo> controls, FieldSnapshot[] snapshot)
        {
            float labelX = 20f;
            var labelHits = ScanVertical(window, startY, endY, labelX, snapshot);

            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Initialise prevKb from the first probe rather than 0.
            // After Phase 3 (ReadTextValues sends SelectAll / Copy), IMGUI native
            // state retains the TextField's kbCtrl — this is not clearable from
            // managed code.  A fresh OnGUI pass then re-applies that kbCtrl even
            // when the mouse is in empty space, creating a spurious transition
            // from 0 → textFieldKb at the very first probe.
            // Using the first probe's value as baseline eliminates the phantom.
            int prevKb = labelHits.Count > 0 ? labelHits[0].KeyboardControl : 0;

            for (int i = 0; i < labelHits.Count; i++)
            {
                var hit = labelHits[i];

                if (hit.KeyboardControl > 0 && hit.KeyboardControl != prevKb)
                {
                    // kbCtrl transition → potential new control at this Y.
                    float controlY = hit.Y;

                    // Check if this Y falls inside any existing control's rect.
                    bool overlaps = false;
                    foreach (var ctrl in controls)
                    {
                        if (controlY >= ctrl.Rect.y &&
                            controlY < ctrl.Rect.y + ctrl.Rect.height)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        float height = Mathf.Min(lineHeight, endY - controlY);
                        controls.Add(new ControlInfo(
                            ControlType.ObjectField,
                            new Rect(0, controlY, windowW, height)));
                    }
                }

                prevKb = hit.KeyboardControl;
            }

            // Sort controls by Y position for consistent ordering.
            controls.Sort((a, b) => a.Rect.y.CompareTo(b.Rect.y));
        }

        // ── State save/restore ──────────────────────────────────────

        private struct FieldSnapshot
        {
            public object Target;
            public FieldInfo Field;
            public object Value;
        }

        private static FieldSnapshot[] SaveState(EditorWindow window)
        {
            var snapshots = new List<FieldSnapshot>();
            AddObjectFields(window, snapshots);

            // For InspectorHostWindow, also snapshot the editor's target (the Component)
            // so that probing doesn't permanently mutate component state.
            if (window is InspectorHostWindow host &&
                host.HostedEditor != null && host.HostedEditor.target != null)
            {
                AddObjectFields(host.HostedEditor.target, snapshots);
            }

            return snapshots.ToArray();
        }

        private static void AddObjectFields(object target, List<FieldSnapshot> snapshots)
        {
            var type = target.GetType();
            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                snapshots.Add(new FieldSnapshot
                {
                    Target = target,
                    Field = field,
                    Value = field.GetValue(target)
                });
            }
        }

        private static void RestoreState(
            EditorWindow window, FieldSnapshot[] snapshot)
        {
            foreach (var s in snapshot)
                s.Field.SetValue(s.Target, s.Value);
        }

        // ── Tab-bar offset ──────────────────────────────────────────

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
