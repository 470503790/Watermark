# Architecture Overview

```
Watermark.Core        ← models (Template, Layers, Transform, ExportSettings), IRenderer
Watermark.Render.Skia ← SkiaRenderer implements IRenderer (SkiaSharp CPU)
Watermark.IO          ← TemplateService (save/load JSON)
Watermark.UI.WinForms ← Editor shell (canvas, insert text/image, export)
```

**Design Goals**
- Cross-platform core (no System.Drawing), SkiaSharp for consistent rendering
- Separation of concerns: UI shell uses renderer via `IRenderer`
- Future-proof: batch pipeline, data binding, template package (ZIP) coming in M2–M3

**Rendering Flow**
1. Decode base image → create surface
2. Draw layers in order (TextLayer/ImageLayer)
3. Encode to PNG/JPG/WebP with export settings
