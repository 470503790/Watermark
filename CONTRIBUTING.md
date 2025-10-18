# Contributing Guide

Thanks for helping improve the Watermark Tool!

## Branching & Commits
- Default branch: `main`
- Feature branches: `feat/<short-topic>` (e.g. `feat/tiling`)
- Fix branches: `fix/<short-topic>`
- Use **Conventional Commits**:
  - `feat: add tiling support`
  - `fix: handle missing fonts gracefully`
  - `docs: update README`
  - `chore: bump SkiaSharp`

## Working with the Code
- Prereqs: .NET 8 SDK, Windows (for WinForms UI), VS2022 or `dotnet` CLI.
- Open `src/Watermark.sln` or launch the WinForms project.
- Prefer small PRs with focused changes. Include screenshots/GIFs for UI changes.

## Code Style
- Enforced via `.editorconfig`. Use 4-space indentation.
- Enable nullable and implicit usings (already set).
- Write XML doc comments for public APIs where it helps.

## Tests
- (Planned) We will add tests in M2/M3. For now, include manual steps in PRs.

## PR Review Checklist
- [ ] Builds in CI (Windows)
- [ ] No crashes on basic flows (open → add layers → export)
- [ ] Exported images match preview expectations
- [ ] README/docs updated when needed
