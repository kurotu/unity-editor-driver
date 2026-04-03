using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniEditWright
{
    /// <summary>
    /// Resolves control rects for UIElements (UI Toolkit) windows.
    /// <para>
    /// <b>Describe mode</b> (controls list pre-populated): fills each control's
    /// <see cref="ControlInfo.Rect"/> by querying the visual tree for matching labels.
    /// </para>
    /// <para>
    /// <b>Scan mode</b> (controls list empty): auto-discovers interactive controls
    /// by traversing the visual tree in document order. Only enabled elements
    /// (<see cref="VisualElement.enabledInHierarchy"/>) are discovered.
    /// </para>
    /// </summary>
    internal sealed class UIElementsResolver : ILayoutResolver
    {
        public void Resolve(EditorWindow window, IList<ControlInfo> controls)
        {
            var root = window.rootVisualElement;

            if (controls.Count == 0)
            {
                DiscoverControls(root, controls);
                return;
            }

            foreach (var control in controls)
            {
                if (control.Type == ControlType.EndToggleGroup)
                {
                    control.Rect = Rect.zero;
                    continue;
                }

                var element = FindElement(root, control.Label, control.Type);
                if (element != null)
                {
                    control.Rect = element.worldBound;
                }
            }
        }

        // ── Auto-discovery (Scan mode) ──────────────────────────────

        private static void DiscoverControls(VisualElement root, IList<ControlInfo> controls)
        {
            TraverseAndDiscover(root, controls);
        }

        /// <summary>
        /// Depth-first traversal that classifies each element as a known control type.
        /// When a control is found, its children are skipped to avoid duplicates
        /// (e.g. the inner TextInput of a TextField).
        /// </summary>
        private static void TraverseAndDiscover(VisualElement element, IList<ControlInfo> controls)
        {
            if (!element.enabledInHierarchy)
                return;

            if (element is TextField tf)
            {
                var info = new ControlInfo(ControlType.TextField, tf.worldBound, tf.value);
                info.Label = tf.label;
                controls.Add(info);
                return;
            }

            if (element is IntegerField intF)
            {
                var info = new ControlInfo(ControlType.IntField, intF.worldBound, intF.value.ToString());
                info.Label = intF.label;
                controls.Add(info);
                return;
            }

            if (element is FloatField floatF)
            {
                var info = new ControlInfo(ControlType.FloatField, floatF.worldBound, floatF.value.ToString());
                info.Label = floatF.label;
                controls.Add(info);
                return;
            }

            if (element is Slider sl)
            {
                var info = new ControlInfo(ControlType.Slider, sl.worldBound, sl.value.ToString());
                info.Label = sl.label;
                controls.Add(info);
                return;
            }

            if (element is Toggle tg)
            {
                var info = new ControlInfo(ControlType.Toggle, tg.worldBound);
                info.Label = tg.label;
                controls.Add(info);
                return;
            }

            if (element is Button btn)
            {
                var info = new ControlInfo(ControlType.Button, btn.worldBound, btn.text);
                info.Label = btn.text;
                controls.Add(info);
                return;
            }

            foreach (var child in element.Children())
            {
                TraverseAndDiscover(child, controls);
            }
        }

        // ── Label-based lookup (Describe mode) ─────────────────────

        private static VisualElement FindElement(VisualElement root, string label, ControlType type)
        {
            switch (type)
            {
                case ControlType.TextField:
                    return root.Query<TextField>().Where(e => e.label == label).First();
                case ControlType.Toggle:
                case ControlType.ToggleGroup:
                    return root.Query<Toggle>().Where(e => e.label == label).First();
                case ControlType.Slider:
                    return root.Query<Slider>().Where(e => e.label == label).First();
                case ControlType.Label:
                    return root.Query<Label>().Where(e => e.text == label).First();
                case ControlType.Button:
                    return root.Query<Button>().Where(e => e.text == label).First();
                case ControlType.IntField:
                    return root.Query<IntegerField>().Where(e => e.label == label).First();
                case ControlType.FloatField:
                    return root.Query<FloatField>().Where(e => e.label == label).First();
                default:
                    return null;
            }
        }
    }
}
