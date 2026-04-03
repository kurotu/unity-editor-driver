using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MyUIElementsComponent))]
public class MyUIElementsComponentEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        var header = new Label("Base Settings");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(header);

        var textField = new TextField("Text Field");
        textField.BindProperty(serializedObject.FindProperty("myString"));
        root.Add(textField);

        var groupToggle = new Toggle("Optional Settings");
        groupToggle.BindProperty(serializedObject.FindProperty("groupEnabled"));
        root.Add(groupToggle);

        var optionalGroup = new VisualElement();
        optionalGroup.SetEnabled(serializedObject.FindProperty("groupEnabled").boolValue);

        var innerToggle = new Toggle("Toggle");
        innerToggle.BindProperty(serializedObject.FindProperty("myBool"));
        optionalGroup.Add(innerToggle);

        var slider = new Slider("Slider", -3f, 3f);
        slider.BindProperty(serializedObject.FindProperty("myFloat"));
        optionalGroup.Add(slider);

        root.Add(optionalGroup);

        groupToggle.RegisterValueChangedCallback(evt => optionalGroup.SetEnabled(evt.newValue));

        return root;
    }
}
