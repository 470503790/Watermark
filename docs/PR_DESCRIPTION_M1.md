# PR: M1 Skeleton + Editor MVP

## Summary
- Initial multi-project structure (Core / Render.Skia / IO / UI.WinForms)
- Basic editor shell:
  - Open base image
  - Insert TextLayer / ImageLayer
  - Move (LMB drag) and Rotate (RMB drag) selected layer
  - Save/Load template (JSON)
  - Export PNG/JPG/WebP

## Out of Scope (follow-ups)
- Resize handles & precise hit-testing
- Tiling watermarks
- Batch export + concurrency + report
- Template package (ZIP)
- Data bindings & expression engine
- QR/Barcode functions
- Tiled (2048/4096) rendering for >8K

## Testing Steps
1. Launch `Watermark.UI.WinForms`
2. File → Open Image (choose a decent resolution sample)
3. Insert → TextLayer / ImageLayer
4. Drag to move; right-drag to rotate
5. Template → Save; reopen app and Template → Load
6. Export → PNG/JPG/WebP; verify image quality and placement

## Screenshots
_(attach before/after or short GIFs)_

## Risks
- Minimal hit-testing (top-most & fixed area). Will be replaced in M1-A.
- No font packaging yet; text layout may differ cross-machine.

## Checklist
- [ ] CI passes
- [ ] Manual smoke test complete
- [ ] Docs updated (README / DEVELOPMENT / ARCHITECTURE)
