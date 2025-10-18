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

## Roadmap
- Batch processing and data bindings (M2-M3) — see `docs/CODEX_TASKS.md`

## License
MIT
 