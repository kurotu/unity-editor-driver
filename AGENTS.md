# AGENTS.md

## Project Overview
EditorDriver is an E2E testing framework for Unity Editor extensions.
It provides a non-invasive API for automating EditorWindow and custom Inspector interactions and capturing screenshots.
**No modifications to the target EditorWindow or Inspector code are required.**
Works with both IMGUI and UIElements (UI Toolkit) windows.

## Architecture

### Package: `Packages/io.github.kurotu.editor-driver/`
- **Editor/Core/**: `Driver` (window & inspector lifecycle), `WindowHandle` (window interaction wrapper), `InspectorHandle` (inspector interaction wrapper), `InspectorHostWindow` (hosts a single Editor in isolation)
- **Editor/Input/**: `InputSimulator` (synthetic Event creation and dispatch, including IMGUI command events)
- **Editor/Screenshot/**: `ScreenshotCapture` (window capture to PNG)
- **Editor/Page/**: `Page` (technology-agnostic control descriptor), `Locator` (interaction API), `ILayoutResolver` interface, `ImguiLayoutResolver` (IMGUI rect computation), `UIElementsResolver` (visual tree queries), `ImguiProber` (auto-discovery via SendEvent probing), `ControlInfo` (control metadata)
- **Tests/Editor/**: NUnit EditMode tests (81 tests)

### Sample Window: `Assets/Editor/MyWindow.cs`
- Example EditorWindow using **plain IMGUI** (no framework dependency)
- Assembly: `SampleWindows.Editor` (no reference to EditorDriver)

### Sample Component + Inspector: `Assets/Runtime/MyComponent.cs` + `Assets/Editor/MyComponentEditor.cs`
- Example MonoBehaviour with a custom IMGUI inspector (no framework dependency)
- Assemblies: `SampleWindows.Runtime` (component) + `SampleWindows.Editor` (inspector)

## Conventions

### Code Style
- `.editorconfig` at project root defines all rules
- Namespace: `EditorDriver` for framework, `EditorDriver.Tests` for tests
- Private fields: `_camelCase` prefix
- Public API: XML doc comments required
- C# 9.0, netstandard2.1

### Testing
- Fully black-box: no reflection into target window fields
- Verification via `Locator.ReadText()` (clipboard-based) and `Locator.CaptureScreenshot()`
- Unity Test Framework (NUnit) EditMode tests
- `[UnityTest]` with `IEnumerator` for tests needing OnGUI context
- Test assembly: `EditorDriver.Tests.Editor`

### Unity-Specific Notes
- `Event.type` getter returns `Ignore` for mouse events outside OnGUI context (Unity 2022.3 limitation). The raw type IS set correctly and `SendEvent` works.
- `GUI.skin` accessors require OnGUI context — use `EditorStyles.*` for layout calculations outside OnGUI.
- `SendEvent` uses GUIView coordinates (includes tab bar). `ImguiLayoutResolver` accounts for this via `DockArea.borderSize.top`.
- IMGUI clipboard operations use `ValidateCommand`/`ExecuteCommand` events (not KeyDown with Ctrl+C).
- Assembly definitions (`.asmdef`) are required for cross-assembly references in Unity.

### Windows/Linux Cross-Platform Notes
- Use `EditorWindowExtensions.SetPosition()` instead of assigning `EditorWindow.position` directly when tests or layout logic depend on the new size immediately. Linux window managers can apply `position` changes asynchronously, but the min/max-size trick in `SetPosition()` makes the resize deterministic on both Windows and Linux.
- For UIElements inspectors, call `InspectorHandle.Repaint()` before reading UI state after mutating serialized data. It walks `IBindable` elements and forces `binding.PreUpdate()`/`binding.Update()`, which avoids stale bindings that otherwise show up on Linux.
- Build temp-file and screenshot paths with `Path.Combine(...)` and `Path.GetTempPath()` rather than hard-coded separators so the same tests work unchanged on Windows and Linux.

## Build & Test Commands
```bash
# Compile
uloop compile --project-path .

# Run all framework tests
uloop run-tests --filter-type assembly --filter-value "EditorDriver.Tests.Editor"

# Run specific test
uloop run-tests --filter-type exact --filter-value "EditorDriver.Tests.PageTests.Describe_ReturnsPageWithControls"
```

## API Quick Reference

The framework provides non-invasive E2E testing for EditorWindow and custom Inspector extensions:

- **Page**: Auto-discovers UI controls via `Page.Scan()` or manual descriptors via `Page.Describe()`. Non-destructive (window state saved/restored).
- **Locator**: Interacts with discovered controls (ReadText, Fill, Toggle, SetSlider, Click, CaptureScreenshot).
- **InspectorHandle**: Opens custom Inspectors in isolation; provides same interaction surface as WindowHandle.

For detailed API documentation and usage examples, see the code comments in `Editor/Page/` and `Editor/Core/`.
