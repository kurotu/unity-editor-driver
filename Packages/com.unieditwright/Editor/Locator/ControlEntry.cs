using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Represents a tracked IMGUI control with its label and rect.
    /// </summary>
    public readonly struct ControlEntry
    {
        public readonly string Label;
        public readonly Rect Rect;
        public readonly ControlType Type;

        public ControlEntry(string label, Rect rect, ControlType type)
        {
            Label = label;
            Rect = rect;
            Type = type;
        }
    }

    public enum ControlType
    {
        Label,
        TextField,
        Toggle,
        Slider,
        ToggleGroup,
        Button,
        Other
    }
}
