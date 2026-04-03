using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniEditWright
{
    /// <summary>
    /// Describes the control layout of an EditorWindow for non-invasive E2E testing.
    /// Technology-agnostic: works for both IMGUI and UIElements windows.
    /// The target window requires zero modifications.
    /// <para>
    /// <b>Auto-discovery (recommended):</b>
    /// <code>
    /// var page = Page.Scan(window);
    /// page.GetByValue("Hello World").Fill("new text");
    /// page.GetByType(ControlType.Toggle, 0).Toggle();
    /// </code>
    /// </para>
    /// <para>
    /// <b>Manual descriptor (backward compat):</b>
    /// <code>
    /// var page = Page.Describe(window, p =&gt;
    /// {
    ///     p.Label("Title");
    ///     p.TextField("Name");
    ///     p.Toggle("Enabled");
    /// });
    /// page.GetByLabel("Name").Fill("hello");
    /// </code>
    /// </para>
    /// </summary>
    public class Page
    {
        private readonly EditorWindow _window;
        private readonly List<ControlInfo> _controls = new List<ControlInfo>();
        private ILayoutResolver _resolver;
        private bool _scanned;

        private Page(EditorWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        /// <summary>
        /// Creates a <see cref="Page"/> for the given window using a fluent descriptor.
        /// Automatically detects whether the window uses IMGUI or UIElements
        /// and selects the appropriate layout resolver.
        /// </summary>
        public static Page Describe(EditorWindow window, Action<Page> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var page = new Page(window);
            configure(page);
            page._resolver = DetectResolver(window);
            page._resolver.Resolve(window, page._controls);
            return page;
        }

        /// <summary>
        /// Creates a <see cref="Page"/> with an explicit <see cref="ILayoutResolver"/>.
        /// Use when the auto-detection does not suit your window.
        /// </summary>
        public static Page Describe(EditorWindow window, Action<Page> configure, ILayoutResolver resolver)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            var page = new Page(window);
            configure(page);
            page._resolver = resolver;
            page._resolver.Resolve(window, page._controls);
            return page;
        }

        /// <summary>
        /// Auto-discovers controls by probing the window at runtime.
        /// No manual descriptor needed — supports dynamic UIs.
        /// <para>
        /// <b>IMGUI windows:</b> Sends synthetic mouse events to find interactive
        /// controls, reads text values via the clipboard. Window state is saved and
        /// restored automatically (non-destructive).
        /// </para>
        /// <para>
        /// <b>UIElements windows:</b> Queries the visual tree for known control types.
        /// </para>
        /// Call <see cref="Refresh"/> to re-scan after the UI changes.
        /// </summary>
        public static Page Scan(EditorWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            var page = new Page(window);
            page._scanned = true;

            var root = window.rootVisualElement;
            if (root != null && HasUIElementsContent(root))
            {
                page._resolver = new UIElementsResolver();
                page._resolver.Resolve(window, page._controls);
            }
            else
            {
                var discovered = ImguiProber.Probe(window);
                page._controls.AddRange(discovered);
            }

            return page;
        }

        /// <summary>All described controls with their computed rects.</summary>
        public IReadOnlyList<ControlInfo> Controls => _controls;

        /// <summary>The target window this page describes.</summary>
        public EditorWindow Window => _window;

        // ── Builder methods ─────────────────────────────────────────

        /// <summary>Describes a Label control.</summary>
        public Page Label(string text, GUIStyle style = null)
        {
            _controls.Add(new ControlInfo(text, ControlType.Label, customStyle: style));
            return this;
        }

        /// <summary>Describes a TextField control.</summary>
        public Page TextField(string label)
        {
            _controls.Add(new ControlInfo(label, ControlType.TextField));
            return this;
        }

        /// <summary>Describes a Toggle control.</summary>
        public Page Toggle(string label)
        {
            _controls.Add(new ControlInfo(label, ControlType.Toggle));
            return this;
        }

        /// <summary>Describes a Slider control.</summary>
        public Page Slider(string label, float min = 0f, float max = 1f)
        {
            _controls.Add(new ControlInfo(label, ControlType.Slider, sliderMin: min, sliderMax: max));
            return this;
        }

        /// <summary>Describes a BeginToggleGroup control.</summary>
        public Page BeginToggleGroup(string label)
        {
            _controls.Add(new ControlInfo(label, ControlType.ToggleGroup));
            return this;
        }

        /// <summary>Describes an EndToggleGroup call.</summary>
        public Page EndToggleGroup()
        {
            _controls.Add(new ControlInfo(null, ControlType.EndToggleGroup));
            return this;
        }

        /// <summary>Describes a Button control.</summary>
        public Page Button(string text)
        {
            _controls.Add(new ControlInfo(text, ControlType.Button));
            return this;
        }

        /// <summary>Describes an IntField control.</summary>
        public Page IntField(string label)
        {
            _controls.Add(new ControlInfo(label, ControlType.IntField));
            return this;
        }

        /// <summary>Describes a FloatField control.</summary>
        public Page FloatField(string label)
        {
            _controls.Add(new ControlInfo(label, ControlType.FloatField));
            return this;
        }

        /// <summary>Describes an ObjectField control (Material, Texture, etc.).</summary>
        public Page ObjectField(string label)
        {
            _controls.Add(new ControlInfo(label, ControlType.ObjectField));
            return this;
        }

        // ── Locator ─────────────────────────────────────────────────

        /// <summary>
        /// Returns a <see cref="Locator"/> for the control with the given label.
        /// </summary>
        /// <exception cref="InvalidOperationException">No control with the given label was found.</exception>
        public Locator GetByLabel(string label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));

            for (int i = 0; i < _controls.Count; i++)
            {
                if (_controls[i].Label == label)
                {
                    return new Locator(_window, _controls[i]);
                }
            }

            throw new InvalidOperationException(
                $"Control with label '{label}' not found. " +
                $"Available labels: [{string.Join(", ", GetAvailableLabels())}]");
        }

        /// <summary>
        /// Returns a <see cref="Locator"/> for the first control whose
        /// <see cref="ControlInfo.Value"/> matches the given text.
        /// Useful with <see cref="Scan"/> where labels are not available.
        /// </summary>
        public Locator GetByValue(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            for (int i = 0; i < _controls.Count; i++)
            {
                if (_controls[i].Value == value)
                    return new Locator(_window, _controls[i]);
            }

            throw new InvalidOperationException(
                $"Control with value '{value}' not found. " +
                $"Available values: [{string.Join(", ", GetAvailableValues())}]");
        }

        /// <summary>
        /// Returns a <see cref="Locator"/> for the <paramref name="index"/>-th control
        /// of the specified <paramref name="type"/> (zero-based).
        /// </summary>
        public Locator GetByType(ControlType type, int index = 0)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            int found = 0;
            for (int i = 0; i < _controls.Count; i++)
            {
                if (_controls[i].Type == type)
                {
                    if (found == index)
                        return new Locator(_window, _controls[i]);
                    found++;
                }
            }

            throw new InvalidOperationException(
                $"Control of type {type} at index {index} not found " +
                $"(found {found} total). Types present: [{string.Join(", ", GetAvailableTypes())}]");
        }

        /// <summary>
        /// Re-computes control layout.
        /// For <see cref="Describe"/> pages, re-applies the <see cref="ILayoutResolver"/>.
        /// For <see cref="Scan"/> pages, re-probes the window (captures dynamic changes).
        /// </summary>
        public void Refresh()
        {
            if (_scanned)
            {
                _controls.Clear();
                var root = _window.rootVisualElement;
                if (root != null && HasUIElementsContent(root))
                {
                    _resolver = new UIElementsResolver();
                    _resolver.Resolve(_window, _controls);
                }
                else
                {
                    var discovered = ImguiProber.Probe(_window);
                    _controls.AddRange(discovered);
                }
            }
            else
            {
                _resolver.Resolve(_window, _controls);
            }
        }

        // ── Technology detection ────────────────────────────────────

        private static ILayoutResolver DetectResolver(EditorWindow window)
        {
            var root = window.rootVisualElement;
            if (root != null && HasUIElementsContent(root))
                return new UIElementsResolver();

            return new ImguiLayoutResolver();
        }

        private static bool HasUIElementsContent(VisualElement root)
        {
            if (root.childCount == 0) return false;
            foreach (var child in root.Children())
            {
                if (!(child is IMGUIContainer))
                    return true;
            }
            return false;
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

        private string[] GetAvailableValues()
        {
            var values = new List<string>();
            foreach (var c in _controls)
            {
                if (c.Value != null)
                    values.Add(c.Value);
            }
            return values.ToArray();
        }

        private string[] GetAvailableTypes()
        {
            var seen = new HashSet<string>();
            foreach (var c in _controls)
                seen.Add(c.Type.ToString());
            var arr = new string[seen.Count];
            seen.CopyTo(arr);
            return arr;
        }
    }
}
