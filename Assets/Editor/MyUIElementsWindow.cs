using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MyUIElementsWindow : EditorWindow
{
    [MenuItem("Window/My UIElements Window")]
    static void Init()
    {
        var window = GetWindow<MyUIElementsWindow>();
        window.Show();
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var header = new Label("Base Settings");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(header);

        var textField = new TextField("Text Field") { value = "Hello World" };
        root.Add(textField);

        var groupToggle = new Toggle("Optional Settings") { value = false };
        root.Add(groupToggle);

        var optionalGroup = new VisualElement();
        optionalGroup.SetEnabled(false);

        var innerToggle = new Toggle("Toggle") { value = true };
        optionalGroup.Add(innerToggle);

        var slider = new Slider("Slider", -3f, 3f) { value = 1.23f };
        optionalGroup.Add(slider);

        root.Add(optionalGroup);

        groupToggle.RegisterValueChangedCallback(evt => optionalGroup.SetEnabled(evt.newValue));
    }
}
