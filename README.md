# Watermark Tool (Client-only) — .NET 8 + WinForms + SkiaSharp

A fast, stable, client-only image watermarking tool. Windows GUI first, with a cross-platform core and renderer.

## Features (M1)
- Text & image layers with move/scale/rotate on canvas
- Template save/load (JSON)
- Single-image export (PNG/JPG/WebP)

## Build Prereqs
- .NET 8 SDK
- Visual Studio 2022 or `dotnet` CLI
- Windows for the WinForms UI
- NuGet packages will restore on first build

## Projects
- `Watermark.Core` — models, template, transforms, bindings (basic), export settings
- `Watermark.Render.Skia` — SkiaSharp renderer for preview/export
- `Watermark.IO` — template save/load, helpers
- `Watermark.UI.WinForms` — editor shell (WinForms, SkiaSharp Views)

> Open `src/Watermark.UI.WinForms/Watermark.UI.WinForms.csproj` in Visual Studio to start.

## Run
- Press F5 from the WinForms project. Use **File → Open Image** to load a base image,
  **Insert → Text/Image** to add watermarks, **Template → Save/Load**, **Export** to save.

## Editor Operations
### Canvas Interactions
- **Select Layer**: Click on any layer to select it
- **Deselect**: Click on empty canvas area
- **Move Layer**: Drag the selected layer body
- **Resize**: Drag any of the 8 resize handles (4 corners + 4 edges)
  - **Shift + Drag**: Proportional resize (maintain aspect ratio)
  - **Alt + Drag**: Resize from center
- **Rotate**: Right-click and drag, or drag the rotate handle (circle above layer)
  - **Shift + Rotate**: Snap to 15° increments
- **Keyboard Nudge**: 
  - **Arrow Keys**: Move selected layer by 1px
  - **Shift + Arrow Keys**: Move selected layer by 10px

### Handle Visual Feedback
- 8 resize handles appear as white squares with dark borders when layer is selected
- Rotate handle appears as a circle above the layer, connected by a line
- Handles highlight when hovered
- Cursor changes to indicate resize/rotate direction
- Selection box shown in light blue (#66A7CF)

### High DPI Support
- Handle sizes scale appropriately on 125%, 150%, and higher DPI displays
- All interactions remain pixel-precise regardless of display scaling


## Roadmap
- Batch processing and data bindings (M2-M3) — see `docs/CODEX_TASKS.md`

## License
MIT
 