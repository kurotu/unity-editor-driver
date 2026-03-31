# AGENTS.md

## Project Overview
UniEditWright is an E2E testing framework for Unity Editor extensions.
It provides a Playwright-like API for automating EditorWindow interactions and capturing screenshots.

## Architecture

### Package: `Packages/com.unieditwright/`
- **Editor/Core/**: `EditorDriver` (window lifecycle), `WindowHandle` (interaction wrapper)
- **Editor/Input/**: `InputSimulator` (synthetic Event creation and dispatch)
- **Editor/Screenshot/**: `ScreenshotCapture` (window capture to PNG)
- **Editor/Locator/**: `Locator` (Playwright-like control locator), `ControlEntry` (control metadata)
- **Editor/Tracking/**: `GUITracker` (IMGUI control tracking wrapper)
- **Tests/Editor/**: NUnit EditMode tests (58 tests)

### Sample Window: `Assets/Editor/MyWindow.cs`
- Example EditorWindow using GUITracker for control tracking
- Assembly: `SampleWindows.Editor`

## Conventions

### Code Style
- `.editorconfig` at project root defines all rules
- Namespace: `UniEditWright` for framework, `UniEditWright.Tests` for tests
- Private fields: `_camelCase` prefix
- Public API: XML doc comments required
- C# 9.0, netstandard2.1

### Testing
- TDD approach: tests written before implementation
- Unity Test Framework (NUnit) EditMode tests
- `[UnityTest]` with `IEnumerator` for tests needing OnGUI context
- Test assembly: `UniEditWright.Tests.Editor`

### Unity-Specific Notes
- `Event.type` getter returns `Ignore` for mouse events outside OnGUI context (Unity 2022.3 limitation). The raw type IS set correctly and `SendEvent` works.
- `GUILayoutUtility.GetLastRect()` cannot be called after `BeginToggleGroup` — use direct entry recording instead.
- Assembly definitions (`.asmdef`) are required for cross-assembly references in Unity.

## Build & Test Commands
```bash
# Compile
uloop compile --project-path .

# Run all framework tests
uloop run-tests --filter-type assembly --filter-value "UniEditWright.Tests.Editor"

# Run specific test
uloop run-tests --filter-type exact --filter-value "UniEditWright.Tests.EditorDriverTests.OpenWindow_ReturnsNonNullHandle"
```

## GUITracker Integration Guide
To make an EditorWindow trackable for E2E tests:
1. Add `using UniEditWright;`
2. In `OnGUI()`, wrap controls with `GUITracker`:
```csharp
void OnGUI()
{
    var t = GUITracker.Begin(this);
    t.Label("Title", EditorStyles.boldLabel);
    myField = t.TextField("Label", myField);
    myBool = t.Toggle("Check", myBool);
    myFloat = t.Slider("Amount", myFloat, 0, 1);
    t.End();
}
```
