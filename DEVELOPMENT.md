# Development Guide

## Getting Started
1. Install **.NET 8 SDK** and **Visual Studio 2022** (or use `dotnet` CLI).
2. Open `src/Watermark.sln`.
3. Set startup project to `Watermark.UI.WinForms` and press F5.

## Project Layout
- `Watermark.Core` — data models (Template, Layers, Transform, ExportSettings), interfaces
- `Watermark.Render.Skia` — SkiaSharp-based renderer (export and preview)
- `Watermark.IO` — template save/load helpers
- `Watermark.UI.WinForms` — Windows UI shell using `SkiaSharp.Views.WindowsForms`

## Useful Paths
- `samples/example-template.json` — a template example
- `docs/CODEX_TASKS.md` — backlog with DoD for M1–M3

## Local Build
```bash
dotnet restore src/Watermark.UI.WinForms/Watermark.UI.WinForms.csproj
dotnet build src/Watermark.UI.WinForms/Watermark.UI.WinForms.csproj -c Release
```

## Troubleshooting
- **Fonts**: if a font is missing on your machine, text layout may differ. We will add font packaging in M2.
- **WebP**: SkiaSharp handles WebP encode/decode, but some viewers may not display WebP; test with PNG/JPG for interoperability.
- **High DPI**: the app enables SystemAware DPI. Report glitches with screenshots + scaling level.
