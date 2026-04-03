using UnityEngine;

/// <summary>
/// Sample MonoBehaviour used as a test target for custom inspector E2E tests.
/// Contains the same types of fields as MyWindow for consistency.
/// </summary>
public class MyComponent : MonoBehaviour
{
    public string myString = "Hello World";
    public bool groupEnabled;
    public bool myBool = true;
    public float myFloat = 1.23f;
}
