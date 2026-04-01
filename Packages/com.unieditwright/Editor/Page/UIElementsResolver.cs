using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniEditWright
{
    /// <summary>
    /// Resolves control rects for UIElements (UI Toolkit) windows
    /// by querying the visual tree for elements matching each descriptor's label and type.
    /// </summary>
    internal sealed class UIElementsResolver : ILayoutResolver
    {
        public void Resolve(EditorWindow window, IList<ControlInfo> controls)
        {
            var root = window.rootVisualElement;

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
