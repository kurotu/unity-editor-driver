using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Describes a single control in a window layout descriptor.
    /// Technology-agnostic: works for both IMGUI and UIElements windows.
    /// </summary>
    public class ControlInfo
    {
        public string Label { get; internal set; }
        public ControlType Type { get; internal set; }
        public GUIStyle CustomStyle { get; }
        public float SliderMin { get; }
        public float SliderMax { get; }

        /// <summary>
        /// Computed rect in GUIView coordinate space (suitable for SendEvent).
        /// Set by <see cref="ILayoutResolver"/> or auto-discovery.
        /// </summary>
        public Rect Rect { get; internal set; }

        /// <summary>
        /// The displayed text value of the control, read via clipboard during
        /// <see cref="Page.Scan"/>. Null when not available (e.g. for toggles).
        /// </summary>
        public string Value { get; internal set; }

        public ControlInfo(string label, ControlType type, GUIStyle customStyle = null,
            float sliderMin = 0f, float sliderMax = 1f)
        {
            Label = label;
            Type = type;
            CustomStyle = customStyle;
            SliderMin = sliderMin;
            SliderMax = sliderMax;
        }

        internal ControlInfo(ControlType type, Rect rect, string value = null)
        {
            Type = type;
            Rect = rect;
            Value = value;
        }
    }

    /// <summary>
    /// Types of controls supported by <see cref="Page"/>.
    /// </summary>
    public enum ControlType
    {
        Label,
        TextField,
        Toggle,
        Slider,
        ToggleGroup,
        EndToggleGroup,
        Button,
        IntField,
        FloatField,
        ObjectField
    }
}
