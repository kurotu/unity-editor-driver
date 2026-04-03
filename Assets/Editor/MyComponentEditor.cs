using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for <see cref="MyComponent"/>.
/// Uses plain IMGUI (no framework dependency), mirroring MyWindow's layout.
/// </summary>
[CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var component = (MyComponent)target;

        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        component.myString = EditorGUILayout.TextField("Text Field", component.myString);
        component.myMaterial = (Material)EditorGUILayout.ObjectField("Material", component.myMaterial, typeof(Material), false);
        component.myTexture = (Texture)EditorGUILayout.ObjectField("Texture", component.myTexture, typeof(Texture), false);

        component.groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", component.groupEnabled);
        component.myBool = EditorGUILayout.Toggle("Toggle", component.myBool);
        component.myFloat = EditorGUILayout.Slider("Slider", component.myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
