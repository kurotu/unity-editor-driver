using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Describes the IMGUI control layout of an EditorWindow for non-invasive E2E testing.
    /// The target window requires zero modifications — the layout is described in test code.
    /// <para>
    /// Usage:
    /// <code>
    /// var page = ImguiPage.Describe(window, p =&gt;
    /// {
    ///     p.Label("Title");
    ///     p.TextField("Name");
    ///     p.Toggle("Enabled");
    /// });
    /// page.GetByLabel("Name").Fill("hello");
    /// </code>
    /// </para>
    /// </summary>
    public class ImguiPage
    {
        private readonly EditorWindow _window;
        private readonly List<ImguiControlInfo> _controls = new List<ImguiControlInfo>();

        private ImguiPage(EditorWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        /// <summary>
        /// Creates an <see cref="ImguiPage"/> for the given window using a fluent descriptor.
        /// Control rects are computed synchronously from IMGUI layout constants.
        /// </summary>
        public static ImguiPage Describe(EditorWindow window, Action<ImguiPage> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var page = new ImguiPage(window);
            configure(page);
            LayoutCalculator.Resolve(window, page._controls);
            return page;
        }

        /// <summary>All described controls with their computed rects.</summary>
        public IReadOnlyList<ImguiControlInfo> Controls => _controls;

        /// <summary>The target window this page describes.</summary>
        public EditorWindow Window => _window;

        // ── Builder methods ─────────────────────────────────────────

        /// <summary>Describes a <c>GUILayout.Label</c> control.</summary>
        public ImguiPage Label(string text, GUIStyle style = null)
        {
            _controls.Add(new ImguiControlInfo(text, ControlType.Label, customStyle: style));
            return this;
        }

        /// <summary>Describes an <c>EditorGUILayout.TextField</c> control.</summary>
        public ImguiPage TextField(string label)
        {
            _controls.Add(new ImguiControlInfo(label, ControlType.TextField));
            return this;
        }

        /// <summary>Describes an <c>EditorGUILayout.Toggle</c> control.</summary>
        public ImguiPage Toggle(string label)
        {
            _controls.Add(new ImguiControlInfo(label, ControlType.Toggle));
            return this;
        }

        /// <summary>Describes an <c>EditorGUILayout.Slider</c> control.</summary>
        public ImguiPage Slider(string label, float min = 0f, float max = 1f)
        {
            _controls.Add(new ImguiControlInfo(label, ControlType.Slider, sliderMin: min, sliderMax: max));
            return this;
        }

        /// <summary>Describes an <c>EditorGUILayout.BeginToggleGroup</c> control.</summary>
        public ImguiPage BeginToggleGroup(string label)
        {
            _controls.Add(new ImguiControlInfo(label, ControlType.ToggleGroup));
            return this;
        }

        /// <summary>Describes an <c>EditorGUILayout.EndToggleGroup</c> call.</summary>
        public ImguiPage EndToggleGroup()
        {
            _controls.Add(new ImguiControlInfo(null, ControlType.EndToggleGroup));
            return this;
        }

        /// <summary>Describes a <c>GUILayout.Button</c> control.</summary>
        public ImguiPage Button(string text)
        {
            _controls.Add(new ImguiControlInfo(text, ControlType.Button));
            return this;
        }

        /// <summary>Describes an <c>EditorGUILayout.IntField</c> control.</summary>
        public ImguiPage IntField(string label)
        {
            _controls.Add(new ImguiControlInfo(label, ControlType.IntField));
            return this;
        }

        /// <summary>Describes an <c>EditorGUILayout.FloatField</c> control.</summary>
        public ImguiPage FloatField(string label)
        {
            _controls.Add(new ImguiControlInfo(label, ControlType.FloatField));
            return this;
        }

        // ── Locator ─────────────────────────────────────────────────

        /// <summary>
        /// Returns an <see cref="ImguiLocator"/> for the control with the given label.
        /// </summary>
        /// <exception cref="InvalidOperationException">No control with the given label was found.</exception>
        public ImguiLocator GetByLabel(string label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));

            for (int i = 0; i < _controls.Count; i++)
            {
                if (_controls[i].Label == label)
                {
                    return new ImguiLocator(_window, _controls[i]);
                }
            }

            throw new InvalidOperationException(
                $"Control with label '{label}' not found. " +
                $"Available labels: [{string.Join(", ", GetAvailableLabels())}]");
        }

        /// <summary>
        /// Re-computes control rects. Call after the window has been resized.
        /// </summary>
        public void Refresh()
        {
            LayoutCalculator.Resolve(_window, _controls);
        }

        private string[] GetAvailableLabels()
        {
            var labels = new List<string>();
            foreach (var c in _controls)
            {
                if (c.Label != null)
                    labels.Add(c.Label);
            }
            return labels.ToArray();
        }
    }
}
