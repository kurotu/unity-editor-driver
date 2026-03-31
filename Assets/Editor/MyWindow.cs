using UnityEngine;
using UnityEditor;
using UniEditWright;

public class MyWindow : EditorWindow
{
    string myString = "Hello World";
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/My Window")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        MyWindow window = (MyWindow)EditorWindow.GetWindow(typeof(MyWindow));
        window.Show();
    }

    void OnGUI()
    {
        var t = GUITracker.Begin(this);

        t.Label("Base Settings", EditorStyles.boldLabel);
        myString = t.TextField("Text Field", myString);

        groupEnabled = t.BeginToggleGroup("Optional Settings", groupEnabled);
        myBool = t.Toggle("Toggle", myBool);
        myFloat = t.Slider("Slider", myFloat, -3, 3);
        t.EndToggleGroup();

        t.End();
    }
}
