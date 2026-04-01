using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Describes a single IMGUI control in a page layout descriptor.
    /// </summary>
    public class ImguiControlInfo
    {
        public string Label { get; }
        public ControlType Type { get; }
        public GUIStyle CustomStyle { get; }
        public float SliderMin { get; }
        public float SliderMax { get; }

        /// <summary>
        /// Computed rect in window-local coordinates. Set by <see cref="LayoutCalculator"/>.
        /// </summary>
        public Rect Rect { get; internal set; }

        public ImguiControlInfo(string label, ControlType type, GUIStyle customStyle = null,
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
    /// Types of IMGUI controls supported by <see cref="ImguiPage"/>.
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
