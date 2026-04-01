using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Describes a single control in a window layout descriptor.
    /// Technology-agnostic: works for both IMGUI and UIElements windows.
    /// </summary>
    public class ControlInfo
    {
        public string Label { get; }
        public ControlType Type { get; }
        public GUIStyle CustomStyle { get; }
        public float SliderMin { get; }
        public float SliderMax { get; }

        /// <summary>
        /// Computed rect in window-local coordinates. Set by <see cref="ILayoutResolver"/>.
        /// </summary>
        public Rect Rect { get; internal set; }

        public ControlInfo(string label, ControlType type, GUIStyle customStyle = null,
            float sliderMin = 0f, float sliderMax = 1f)
        {
            Label = label;
            Type = type;
            CustomStyle = customStyle;
            SliderMin = sliderMin;
            SliderMax = sliderMax;
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
        FloatField
    }
}
