using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EditorDriver
{
    /// <summary>
    /// Resolves control rects for an <see cref="EditorWindow"/>.
    /// Implementations are technology-specific (IMGUI, UIElements).
    /// Rects are in GUIView coordinate space (suitable for SendEvent).
    /// </summary>
    public interface ILayoutResolver
    {
        /// <summary>
        /// Fills the <see cref="ControlInfo.Rect"/> of each control in the list.
        /// </summary>
        void Resolve(EditorWindow window, IList<ControlInfo> controls);
    }

    /// <summary>
    /// Resolves rects for IMGUI windows by replicating EditorGUILayout's vertical stacking.
    /// Rects are in GUIView coordinate space so that <see cref="EditorWindow.SendEvent"/>
    /// hits the correct controls.
    /// </summary>
    internal sealed class ImguiLayoutResolver : ILayoutResolver
    {
        public void Resolve(EditorWindow window, IList<ControlInfo> controls)
        {
            float windowWidth = window.position.width;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float contentYOffset = GetContentYOffset(window);

            float y = contentYOffset;
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

                if (prevStyle != null)
                {
                    float gap = Mathf.Max(prevStyle.margin.bottom, currentStyle.margin.top);
                    y += gap;
                }
                // First control: no extra top margin (the implicit GUILayout group absorbs it)

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

        /// <summary>
        /// Returns the Y offset from the top of the GUIView to the content area.
        /// This is typically the tab-bar height for docked windows.
        /// </summary>
        private static float GetContentYOffset(EditorWindow window)
        {
            try
            {
                // EditorWindow.m_Parent is the hosting View (DockArea for docked windows).
                // DockArea.borderSize.top gives the tab-bar height.
                var parentField = typeof(EditorWindow).GetField(
                    "m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
                if (parentField == null) return 0f;

                var parent = parentField.GetValue(window);
                if (parent == null) return 0f;

                var borderProp = parent.GetType().GetProperty(
                    "borderSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (borderProp == null) return 0f;

                var border = borderProp.GetValue(parent) as RectOffset;
                return border?.top ?? 0f;
            }
            catch
            {
                return 0f;
            }
        }

        private static GUIStyle GetLayoutStyle(ControlInfo control)
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
                ControlType.ObjectField => EditorStyles.objectField,
                _ => EditorStyles.label,
            };
        }
    }
}
