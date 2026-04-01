using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniEditWright
{
    /// <summary>
    /// Computes IMGUI control rects from a layout descriptor using EditorGUI layout constants.
    /// Replicates Unity's EditorGUILayout vertical stacking algorithm.
    /// </summary>
    internal static class LayoutCalculator
    {
        /// <summary>
        /// Resolves the <see cref="ImguiControlInfo.Rect"/> for each control in the list
        /// based on the target window's dimensions and IMGUI layout constants.
        /// </summary>
        public static void Resolve(EditorWindow window, IList<ImguiControlInfo> controls)
        {
            float windowWidth = window.position.width;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            float y = 0f;
            int groupDepth = 0;

            GUIStyle prevStyle = null;

            for (int i = 0; i < controls.Count; i++)
            {
                var control = controls[i];

                if (control.Type == ControlType.EndToggleGroup)
                {
                    if (groupDepth > 0) groupDepth--;
                    control.Rect = Rect.zero;
                    prevStyle = null;
                    continue;
                }

                GUIStyle currentStyle = GetLayoutStyle(control);

                // Compute vertical spacing via margin collapsing (same as GUILayout)
                if (prevStyle != null)
                {
                    float gap = Mathf.Max(prevStyle.margin.bottom, currentStyle.margin.top);
                    y += gap;
                }
                else
                {
                    y += currentStyle.margin.top;
                }

                float indent = groupDepth > 0 ? EditorGUI.indentLevel * 15f : 0f;
                float leftMargin = currentStyle.margin.left + indent;
                float rightMargin = currentStyle.margin.right;
                float x = leftMargin;
                float width = windowWidth - leftMargin - rightMargin;
                float height = lineHeight;

                control.Rect = new Rect(x, y, width, height);

                y += height;
                prevStyle = currentStyle;

                if (control.Type == ControlType.ToggleGroup)
                {
                    groupDepth++;
                }
            }
        }

        private static GUIStyle GetLayoutStyle(ImguiControlInfo control)
        {
            if (control.CustomStyle != null)
                return control.CustomStyle;

            return control.Type switch
            {
                ControlType.Label => EditorStyles.label,
                ControlType.TextField => EditorStyles.numberField,
                ControlType.Toggle => EditorStyles.toggle,
                ControlType.Slider => EditorStyles.numberField,
                ControlType.ToggleGroup => EditorStyles.toggle,
                ControlType.Button => EditorStyles.miniButton,
                ControlType.IntField => EditorStyles.numberField,
                ControlType.FloatField => EditorStyles.numberField,
                _ => EditorStyles.label,
            };
        }
    }
}
