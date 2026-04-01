# AGENTS.md

## Project Overview
UniEditWright is an E2E testing framework for Unity Editor extensions.
It provides a Playwright-like API for automating EditorWindow interactions and capturing screenshots.
**No modifications to the target EditorWindow code are required.**
Works with both IMGUI and UIElements (UI Toolkit) windows.

## Architecture

### Package: `Packages/com.unieditwright/`
- **Editor/Core/**: `EditorDriver` (window lifecycle), `WindowHandle` (interaction wrapper)
- **Editor/Input/**: `InputSimulator` (synthetic Event creation and dispatch, including IMGUI command events)
- **Editor/Screenshot/**: `ScreenshotCapture` (window capture to PNG)
- **Editor/Page/**: `Page` (technology-agnostic control descriptor), `Locator` (Playwright-like interaction), `ILayoutResolver` interface, `ImguiLayoutResolver` (IMGUI rect computation), `UIElementsResolver` (visual tree queries), `ControlInfo` (control metadata)
- **Tests/Editor/**: NUnit EditMode tests (59 tests)

### Sample Window: `Assets/Editor/MyWindow.cs`
- Example EditorWindow using **plain IMGUI** (no framework dependency)
- Assembly: `SampleWindows.Editor` (no reference to UniEditWright)

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

1. Open the window via `EditorDriver`
2. Describe its layout with `Page.Describe` (auto-detects IMGUI vs UIElements)
3. Interact via `Locator` (black-box: ReadText, Fill, Toggle, SetSlider, CaptureScreenshot)

```csharp
// The target window uses plain IMGUI — no framework dependency
// void OnGUI() { EditorGUILayout.TextField("Name", name); ... }

var handle = driver.OpenWindow<MyWindow>();
yield return null;

var page = Page.Describe(handle.Window, p =>
{
    p.Label("Base Settings", EditorStyles.boldLabel);
    p.TextField("Text Field");
    p.BeginToggleGroup("Optional Settings");
    p.Toggle("Toggle");
    p.Slider("Slider", -3, 3);
    p.EndToggleGroup();
});

// Read displayed text (clipboard-based, no reflection)
var text = page.GetByLabel("Text Field").ReadText();

// Interact
page.GetByLabel("Text Field").Fill("new value");
page.GetByLabel("Optional Settings").Toggle();
page.GetByLabel("Slider").SetSlider(0.5f);
```
