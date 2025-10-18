# M1-A Canvas Interaction Testing Guide

## Overview
This document outlines the testing procedures for the M1-A canvas interaction enhancements. All features have been implemented and built successfully.

## Build & Run
```bash
cd src/Watermark.UI.WinForms
dotnet build -p:EnableWindowsTargeting=true
dotnet run -p:EnableWindowsTargeting=true
```

Or open the solution in Visual Studio 2022 on Windows and press F5.

## Test Cases

### 1. Selection and Deselection
**Steps:**
1. Open the application
2. File → Open Image (select any image)
3. Insert → Text Layer
4. Click on the text layer - should select it with 8 handles + rotate handle visible
5. Click on empty canvas area - selection should disappear
6. Click on layer again - should re-select

**Expected Result:**
- Selection box appears in light blue (#66A7CF)
- 8 white square handles with dark borders at corners and edges
- Rotate handle (circle) connected by a line above the layer
- Click on empty area deselects the layer

### 2. Handle Hover States
**Steps:**
1. Select a layer
2. Move mouse over each handle without clicking
3. Move mouse over the rotate handle

**Expected Result:**
- Handles highlight with semi-transparent blue overlay when hovered
- Cursor changes to appropriate shape:
  - Corner handles (TL/BR): SizeNWSE cursor
  - Corner handles (TR/BL): SizeNESW cursor
  - Top/Bottom handles: SizeNS cursor
  - Left/Right handles: SizeWE cursor
  - Rotate handle: Hand cursor
  - Layer body: SizeAll cursor

### 3. Resize with Corner Handles
**Steps:**
1. Select a layer
2. Drag each corner handle (TL, TR, BR, BL)
3. Hold Shift and drag a corner - should maintain aspect ratio
4. Hold Alt and drag a corner - should resize from center

**Expected Result:**
- Layer resizes from the opposite corner as pivot
- Shift: maintains aspect ratio
- Alt: resizes symmetrically from center
- Minimum size constraint (4px) prevents tiny layers

### 4. Resize with Edge Handles
**Steps:**
1. Select a layer
2. Drag each edge handle (Top, Right, Bottom, Left)
3. Hold Shift while dragging - should affect both width and height proportionally

**Expected Result:**
- Layer resizes in one dimension
- Opposite edge remains fixed
- Shift: maintains aspect ratio by adjusting both dimensions

### 5. Rotation
**Steps:**
1. Select a layer
2. Drag the rotate handle (circle above layer)
3. Hold Shift while rotating

**Expected Result:**
- Layer rotates around its center
- Without Shift: smooth continuous rotation
- With Shift: rotation snaps to 15° increments (0°, 15°, 30°, 45°, etc.)
- Rotate handle distance from layer is approximately 24px

### 6. Rotation-Aware Hit Testing
**Steps:**
1. Select a layer
2. Rotate it to 45° using the rotate handle
3. Try clicking on handles - they should still be clickable
4. Rotate to 135°
5. Try clicking on handles again

**Expected Result:**
- All handles remain clickable at any rotation angle
- Hover states work correctly on rotated layers
- Click detection on layer body works at any angle

### 7. Keyboard Nudging (Arrow Keys)
**Steps:**
1. Select a layer
2. Press Left arrow key multiple times
3. Press Right arrow key
4. Press Up arrow key
5. Press Down arrow key
6. Hold Shift and press each arrow key

**Expected Result:**
- Without Shift: layer moves 1 pixel per keypress
- With Shift: layer moves 10 pixels per keypress
- Movement is smooth and immediate
- Works regardless of layer rotation

### 8. Combined Modifier Keys
**Steps:**
1. Select a layer
2. Hold both Shift and Alt, then drag a corner handle

**Expected Result:**
- Layer resizes proportionally (Shift) from center (Alt)
- Both modifiers work together correctly

### 9. High DPI Testing (125%, 150%)
**Steps:**
1. Change Windows display scaling to 125% (Settings → Display → Scale)
2. Launch application
3. Select a layer and check handle sizes
4. Test handle hover and click
5. Repeat at 150% scaling

**Expected Result:**
- Handles remain visually consistent size (not too small or large)
- Handle click areas scale appropriately
- All strokes and outlines remain crisp
- No visual artifacts or scaling issues

### 10. Multi-Layer Workflow
**Steps:**
1. Open image
2. Insert → Text Layer (creates first layer)
3. Insert → Text Layer (creates second layer)
4. Click on first layer - should select it
5. Click on second layer - should switch selection
6. Use keyboard to move second layer
7. Click empty area - should deselect

**Expected Result:**
- Only one layer selected at a time
- Selection switches correctly between layers
- Z-order respected (top layer selected first)

### 11. Export Consistency
**Steps:**
1. Create a layer with specific position (e.g., X=100, Y=100)
2. Resize to specific dimensions
3. Rotate to specific angle (e.g., 45°)
4. File → Export to PNG
5. Open exported file and verify position/size/rotation matches preview

**Expected Result:**
- Exported result matches canvas preview exactly
- Position accuracy: ±1px
- Rotation accuracy: ±0.5°
- Dimensions match what was set during editing

## Performance Requirements

### Frame Rate
- Canvas redraws should be ≥55 FPS during normal interaction
- No visible lag when moving cursor over handles
- Smooth redraw during drag operations

### Event Handling
- Mouse event processing: <10ms per event
- Keyboard event processing: <5ms per event
- No freezing or stuttering during rapid input

## Known Limitations
- Multi-selection not implemented (M1-A scope)
- Undo/Redo not implemented (M4 milestone)
- Complex text multi-line wrapping uses approximate bounds (M1-B milestone)
- Tiling patterns not supported yet (M1-C milestone)

## Visual Reference
All handles should appear as:
- Size: ~6px squares (logical pixels, scaled for DPI)
- Color: White fill with dark (#3C3C3C) border
- Selection box: #66A7CF color
- Hover: Semi-transparent blue highlight (+2px expansion)
- Rotate handle: Circle with connecting line, ~24px from layer edge

## Regression Checks
Ensure these existing features still work:
- File → Open Image
- Insert → Text/Image Layer
- Template → Save/Load
- File → Export (PNG/JPG/WebP)
- Basic drag to move layer
- Layer visibility and opacity

## Bug Reporting
If any issues are found during testing, report with:
1. Exact steps to reproduce
2. Expected vs actual behavior
3. Windows version and DPI setting
4. Screenshot or screen recording if applicable
