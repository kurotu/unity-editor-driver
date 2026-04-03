# AGENTS.md

## Project Overview
UniEditWright is an E2E testing framework for Unity Editor extensions.
It provides a Playwright-like API for automating EditorWindow and custom Inspector interactions and capturing screenshots.
**No modifications to the target EditorWindow or Inspector code are required.**
Works with both IMGUI and UIElements (UI Toolkit) windows.

## Architecture

### Package: `Packages/com.unieditwright/`
- **Editor/Core/**: `EditorDriver` (window & inspector lifecycle), `WindowHandle` (window interaction wrapper), `InspectorHandle` (inspector interaction wrapper), `InspectorHostWindow` (hosts a single Editor in isolation)
- **Editor/Input/**: `InputSimulator` (synthetic Event creation and dispatch, including IMGUI command events)
- **Editor/Screenshot/**: `ScreenshotCapture` (window capture to PNG)
- **Editor/Page/**: `Page` (technology-agnostic control descriptor), `Locator` (Playwright-like interaction), `ILayoutResolver` interface, `ImguiLayoutResolver` (IMGUI rect computation), `UIElementsResolver` (visual tree queries), `ImguiProber` (auto-discovery via SendEvent probing), `ControlInfo` (control metadata)
- **Tests/Editor/**: NUnit EditMode tests (81 tests)

### Sample Window: `Assets/Editor/MyWindow.cs`
- Example EditorWindow using **plain IMGUI** (no framework dependency)
- Assembly: `SampleWindows.Editor` (no reference to UniEditWright)

### Sample Component + Inspector: `Assets/Runtime/MyComponent.cs` + `Assets/Editor/MyComponentEditor.cs`
- Example MonoBehaviour with a custom IMGUI inspector (no framework dependency)
- Assemblies: `SampleWindows.Runtime` (component) + `SampleWindows.Editor` (inspector)

## Conventions

### Code Style
- `.editorconfig` at project root defines all rules
- Namespace: `UniEditWright` for framework, `UniEditWright.Tests` for tests
- Private fields: `_camelCase` prefix
- Public API: XML doc comments required
- C# 9.0, netstandard2.1

### Testing
- Fully black-box: no reflection into target window fields
- Verification via `Locator.ReadText()` (clipboard-based) and `Locator.CaptureScreenshot()`
- Unity Test Framework (NUnit) EditMode tests
- `[UnityTest]` with `IEnumerator` for tests needing OnGUI context
- Test assembly: `UniEditWright.Tests.Editor`

### Unity-Specific Notes
- `Event.type` getter returns `Ignore` for mouse events outside OnGUI context (Unity 2022.3 limitation). The raw type IS set correctly and `SendEvent` works.
- `GUI.skin` accessors require OnGUI context — use `EditorStyles.*` for layout calculations outside OnGUI.
- `SendEvent` uses GUIView coordinates (includes tab bar). `ImguiLayoutResolver` accounts for this via `DockArea.borderSize.top`.
- IMGUI clipboard operations use `ValidateCommand`/`ExecuteCommand` events (not KeyDown with Ctrl+C).
- Assembly definitions (`.asmdef`) are required for cross-assembly references in Unity.

## Build & Test Commands
```bash
# Compile
uloop compile --project-path .

# Run all framework tests
uloop run-tests --filter-type assembly --filter-value "UniEditWright.Tests.Editor"

# Run specific test
uloop run-tests --filter-type exact --filter-value "UniEditWright.Tests.PageTests.Describe_ReturnsPageWithControls"
```

## Page Usage Guide (Non-Invasive E2E Testing)
To E2E test any EditorWindow **without modifying it**:

### Auto-Discovery with `Page.Scan` (Recommended)
No manual descriptor needed — works with dynamic UIs where controls appear/disappear.

```csharp
var handle = driver.OpenWindow<MyWindow>();
yield return null;

// Auto-discover all interactive controls
var page = Page.Scan(handle.Window);

// Find controls by displayed text value
var textField = page.GetByValue("Hello World");
var text = textField.ReadText();
textField.Fill("new value");

// Find controls by type (zero-based index)
page.GetByType(ControlType.Toggle, 0).Toggle();

// After UI changes, re-scan to discover new controls
page.Refresh();
var newControls = page.Controls; // IReadOnlyList<ControlInfo>
```

**How it works (IMGUI):** Sends synthetic mouse events at vertical intervals, checks `GUIUtility.keyboardControl` to find interactive regions, reads text via clipboard. Window state is saved/restored automatically (non-destructive).

### Manual Descriptor with `Page.Describe`
When you know the exact layout and need label-based access:

```csharp
var page = Page.Describe(handle.Window, p =>
{
    p.Label("Base Settings", EditorStyles.boldLabel);
    p.TextField("Text Field");
    p.BeginToggleGroup("Optional Settings");
    p.Toggle("Toggle");
    p.Slider("Slider", -3, 3);
    p.EndToggleGroup();
});

// Find by label name
page.GetByLabel("Text Field").Fill("new value");
page.GetByLabel("Optional Settings").Toggle();
```

### Locator API
| Method | Description |
|--------|-------------|
| `ReadText()` | Read displayed text via clipboard (SelectAll → Copy) |
| `Fill(string)` | Clear field and type new text |
| `Toggle()` | Click a toggle/checkbox control |
| `SetSlider(float)` | Set slider value (type into numeric field) |
| `CaptureScreenshot(path)` | Save control area as PNG |

### Page Query API
| Method | Works with | Description |
|--------|-----------|-------------|
| `GetByLabel(string)` | `Describe` | Find control by IMGUI/UIElements label |
| `GetByValue(string)` | `Scan` | Find control by displayed text value |
| `GetByType(ControlType, int)` | Both | Find Nth control of a given type |
| `Controls` | Both | Read-only list of all discovered controls |
| `Refresh()` | Both | Re-compute layout / re-scan for dynamic changes |

## Inspector Usage Guide (Non-Invasive E2E Testing)
To E2E test any custom Inspector (Editor subclass) **without modifying it**:

### Opening an Inspector
`EditorDriver.OpenInspector<TComponent>()` creates a temporary GameObject, adds the component, creates the custom Editor, and hosts it in an isolated window.

```csharp
var driver = new EditorDriver();
var handle = driver.OpenInspector<MyComponent>();
yield return null;

// With explicit editor type
var handle2 = driver.OpenInspector<MyComponent, MyComponentEditor>();
```

### InspectorHandle API
`InspectorHandle` provides the same interaction surface as `WindowHandle`:

| Property / Method | Description |
|-------------------|-------------|
| `Window` | The host EditorWindow (pass to `Page.Scan`) |
| `Editor` | The custom `UnityEditor.Editor` instance |
| `Component` | The target Component |
| `GameObject` | The temporary GameObject |
| `Screenshot(path)` | Capture screenshot |
| `Repaint()` | Force repaint |

### Using Page.Scan with Inspectors
Works identically to EditorWindow — pass `handle.Window`:

```csharp
var handle = driver.OpenInspector<MyComponent>();
yield return null;

var page = Page.Scan(handle.Window);
page.GetByValue("Hello World").Fill("new text");
page.GetByType(ControlType.Toggle, 0).Toggle();
handle.Repaint();
yield return null;

page.Refresh();
```

### Cleanup
```csharp
driver.CloseInspector(handle);  // Destroys window + GameObject
driver.CloseAll();              // Cleans up all windows and GameObjects
driver.Dispose();               // Same as CloseAll (IDisposable)
```
