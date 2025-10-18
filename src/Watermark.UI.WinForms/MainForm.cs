using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Watermark.Core;
using Watermark.IO;
using Watermark.Render.Skia;

namespace Watermark.UI.WinForms
{
    public class MainForm : Form
    {
        private readonly SKControl _canvas;
        private Template _template = new Template();
        private string? _currentImagePath;
        private readonly SkiaRenderer _renderer = new();
        private LayerBase? _selected;
        private readonly Dictionary<LayerBase, LayerVisualInfo> _visualCache = new();
        private HandleGrip _activeGrip = HandleGrip.None;
        private HandleGrip _hoverGrip = HandleGrip.None;
        private DragState? _dragState;
        private int _lastCanvasWidth;
        private int _lastCanvasHeight;
        private string _lastBaseDir = Environment.CurrentDirectory;

        public MainForm()
        {
            Text = "Watermark Tool - M1 Preview";
            Width = 1200; Height = 800;

            var menu = new MenuStrip();
            var file = new ToolStripMenuItem("文件");
            file.DropDownItems.Add(new ToolStripMenuItem("打开图片", null, (_,__) => OpenImage()));
            file.DropDownItems.Add(new ToolStripMenuItem("导出", null, (_,__) => Export()));
            file.DropDownItems.Add(new ToolStripMenuItem("退出", null, (_,__) => Close()));
            var insert = new ToolStripMenuItem("插入");
            insert.DropDownItems.Add(new ToolStripMenuItem("文本层", null, (_,__) => AddText()));
            insert.DropDownItems.Add(new ToolStripMenuItem("图片层", null, (_,__) => AddImage()));
            var template = new ToolStripMenuItem("模板");
            template.DropDownItems.Add(new ToolStripMenuItem("保存模板", null, (_,__) => SaveTemplate()));
            template.DropDownItems.Add(new ToolStripMenuItem("加载模板", null, (_,__) => LoadTemplate()));
            menu.Items.Add(file);
            menu.Items.Add(insert);
            menu.Items.Add(template);
            MainMenuStrip = menu;
            Controls.Add(menu);

            _canvas = new SKControl { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.FromArgb(30,30,30) };
            _canvas.PaintSurface += CanvasOnPaintSurface;
            _canvas.MouseDown += CanvasOnMouseDown;
            _canvas.MouseMove += CanvasOnMouseMove;
            _canvas.MouseUp += CanvasOnMouseUp;
            Controls.Add(_canvas);

            DoubleBuffered = true;
            KeyPreview = true;
        }

        private bool _dragging = false;
        private SKPoint _hoverPoint;
        private void CanvasOnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
            {
                return;
            }

            EnsureVisuals();
            var (layer, grip) = HitTestWithHandles(e.Location, e.Button == MouseButtons.Right);
            
            // Click on empty canvas to deselect
            if (layer == null && e.Button == MouseButtons.Left)
            {
                _selected = null;
                _canvas.Invalidate();
                return;
            }
            
            _selected = layer;
            _activeGrip = grip;
            _dragging = _selected != null && grip != HandleGrip.None;

            if (_dragging && _selected != null && _visualCache.TryGetValue(_selected, out var info))
            {
                var start = new SKPoint(e.X, e.Y);
                _dragState = new DragState
                {
                    InitialMouse = start,
                    Visual = info,
                    InitialCorners = info.Corners.ToArray(),
                    InitialWidth = info.Width,
                    InitialHeight = info.Height,
                    InitialRotation = info.Transform.Rotation,
                    InitialX = info.ParsedX,
                    InitialY = info.ParsedY,
                    WidthWasAuto = info.WidthAuto,
                    HeightWasAuto = info.HeightAuto,
                    InitialCenter = info.Center,
                    BaseWidth = info.BaseWidth,
                    BaseHeight = info.BaseHeight
                };
            }
            else
            {
                _dragState = null;
            }

            _canvas.Invalidate();
        }
        private void CanvasOnMouseMove(object? sender, MouseEventArgs e)
        {
            _hoverPoint = new SKPoint(e.X, e.Y);
            
            if (!_dragging || _selected == null || _dragState == null)
            {
                // Update cursor and hover state based on hover
                EnsureVisuals();
                var (_, grip) = HitTestWithHandles(e.Location, false);
                if (_hoverGrip != grip)
                {
                    _hoverGrip = grip;
                    _canvas.Invalidate();
                }
                UpdateCursor(e.Location);
                return;
            }

            var current = new SKPoint(e.X, e.Y);
            switch (_activeGrip)
            {
                case HandleGrip.Body:
                    ApplyMove(current);
                    break;
                case HandleGrip.Rotate:
                    ApplyRotate(current);
                    break;
                case HandleGrip.TopLeft:
                case HandleGrip.Top:
                case HandleGrip.TopRight:
                case HandleGrip.Right:
                case HandleGrip.BottomRight:
                case HandleGrip.Bottom:
                case HandleGrip.BottomLeft:
                case HandleGrip.Left:
                    ApplyScale(current, _activeGrip);
                    break;
            }
            _canvas.Invalidate();
        }
        private void CanvasOnMouseUp(object? sender, MouseEventArgs e)
        {
            _dragging = false;
            _activeGrip = HandleGrip.None;
            _dragState = null;
            UpdateCursor(e.Location);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_selected == null) return base.ProcessCmdKey(ref msg, keyData);

            var shift = (keyData & Keys.Shift) != 0;
            var delta = shift ? 10f : 1f;
            var key = keyData & ~Keys.Shift;

            switch (key)
            {
                case Keys.Left:
                    NudgeLayer(_selected, -delta, 0);
                    return true;
                case Keys.Right:
                    NudgeLayer(_selected, delta, 0);
                    return true;
                case Keys.Up:
                    NudgeLayer(_selected, 0, -delta);
                    return true;
                case Keys.Down:
                    NudgeLayer(_selected, 0, delta);
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void NudgeLayer(LayerBase layer, float dx, float dy)
        {
            var t = layer.Transform;
            if (Utils.TryParsePercentOrNumber(t.X, _lastCanvasWidth, out var x))
            {
                t.X = FormatNumber(x + dx);
            }
            if (Utils.TryParsePercentOrNumber(t.Y, _lastCanvasHeight, out var y))
            {
                t.Y = FormatNumber(y + dy);
            }
            _canvas.Invalidate();
        }

        private void UpdateCursor(System.Drawing.Point point)
        {
            if (_dragging)
            {
                // Keep current cursor during drag
                return;
            }

            EnsureVisuals();
            var (layer, grip) = HitTestWithHandles(point, false);
            
            _canvas.Cursor = grip switch
            {
                HandleGrip.Body => Cursors.SizeAll,
                HandleGrip.Rotate => Cursors.Hand,
                HandleGrip.TopLeft or HandleGrip.BottomRight => Cursors.SizeNWSE,
                HandleGrip.TopRight or HandleGrip.BottomLeft => Cursors.SizeNESW,
                HandleGrip.Top or HandleGrip.Bottom => Cursors.SizeNS,
                HandleGrip.Left or HandleGrip.Right => Cursors.SizeWE,
                _ => Cursors.Default
            };
        }

        private void CanvasOnPaintSurface(object? sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(new SkiaSharp.SKColor(32,32,32));
            if (_currentImagePath != null && File.Exists(_currentImagePath))
            {
                using var bmp = SkiaSharp.SKBitmap.Decode(_currentImagePath);
                if (bmp != null)
                    canvas.DrawBitmap(bmp, 0, 0);
            }
            var baseDir = GetBaseDirectory();
            SkiaRenderer.DrawLayers(canvas, _template, e.Info.Width, e.Info.Height, baseDir);

            UpdateVisualCache(e.Info.Width, e.Info.Height, baseDir);
            if (_selected != null && _visualCache.TryGetValue(_selected, out var visual))
            {
                DrawSelection(canvas, visual);
            }
        }

        private void OpenImage()
        {
            using var ofd = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _currentImagePath = ofd.FileName;
                _template.Canvas.Width = 0; // derived from image currently
                _canvas.Invalidate();
            }
        }

        private void AddText()
        {
            var tl = new TextLayer
            {
                Name = "文本",
                Text = "双击编辑",
                Style = new TextStyle { FontFamily = "Arial", FontSize = 48, FillColor = "#FFFFFFFF", TextAlign = "left" },
                Transform = new TransformSpec { X = "100", Y = "100", Anchor = Watermark.Core.Anchor.TL }
            };
            _template.Layers.Add(tl);
            _selected = tl;
            _canvas.Invalidate();
        }

        private void AddImage()
        {
            using var ofd = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var rel = MakeRelative(ofd.FileName);
                var il = new ImageLayer
                {
                    Name = "图片",
                    Source = new ImageSource { Type = "file", Value = rel },
                    Transform = new TransformSpec { X = "200", Y = "200", Anchor = Watermark.Core.Anchor.TL }
                };
                _template.Layers.Add(il);
                _selected = il;
                _canvas.Invalidate();
            }
        }

        private string MakeRelative(string path)
        {
            if (_currentImagePath == null) return path;
            var baseDir = Path.GetDirectoryName(_currentImagePath)!;
            try {
                var uri1 = new Uri(baseDir + Path.DirectorySeparatorChar);
                var uri2 = new Uri(path);
                return Uri.UnescapeDataString(uri1.MakeRelativeUri(uri2).ToString().Replace('/', Path.DirectorySeparatorChar));
            } catch { return path; }
        }

        private void SaveTemplate()
        {
            using var sfd = new SaveFileDialog { Filter = "Template (*.json)|*.json" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                TemplateService.Save(sfd.FileName, _template);
                MessageBox.Show("模板已保存。");
            }
        }

        private void LoadTemplate()
        {
            using var ofd = new OpenFileDialog { Filter = "Template (*.json)|*.json" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _template = TemplateService.Load(ofd.FileName);
                _canvas.Invalidate();
            }
        }

        private void Export()
        {
            if (_currentImagePath == null) { MessageBox.Show("请先打开一张图片"); return; }
            using var sfd = new SaveFileDialog { Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|WebP (*.webp)|*.webp", FileName = Path.GetFileNameWithoutExtension(_currentImagePath) + "_wm.png" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var settings = new ExportSettings();
                if (sfd.FilterIndex == 1) settings.Format = "png";
                else if (sfd.FilterIndex == 2) settings.Format = "jpg";
                else settings.Format = "webp";
                _renderer.RenderToFile(_template, _currentImagePath, sfd.FileName, settings);
                MessageBox.Show("已导出：" + sfd.FileName);
            }
        }

        private string GetBaseDirectory() => _currentImagePath != null ? Path.GetDirectoryName(_currentImagePath)! : Environment.CurrentDirectory;

        private (LayerBase? layer, HandleGrip grip) HitTestWithHandles(System.Drawing.Point point, bool forceRotate)
        {
            var pt = new SKPoint(point.X, point.Y);
            for (int i = _template.Layers.Count - 1; i >= 0; i--)
            {
                var layer = _template.Layers[i];
                if (!layer.Visible) continue;
                if (!_visualCache.TryGetValue(layer, out var visual)) continue;

                if (forceRotate)
                {
                    var dpiScale = DeviceDpi / 96f;
                    var hitRadius = 10f * dpiScale;
                    var dist = Distance(pt, visual.RotateHandle);
                    if (dist <= hitRadius)
                        return (layer, HandleGrip.Rotate);
                }

                var grip = HitTestHandles(visual, pt);
                if (grip != HandleGrip.None)
                    return (layer, grip);

                if (PointInPolygon(pt, visual.Corners))
                    return (layer, HandleGrip.Body);
            }
            return (null, HandleGrip.None);
        }

        private HandleGrip HitTestHandles(LayerVisualInfo visual, SKPoint pt)
        {
            var dpiScale = DeviceDpi / 96f;
            var hitRadius = 10f * dpiScale;
            
            foreach (var handle in visual.Handles)
            {
                if (Distance(pt, handle.Position) <= hitRadius)
                    return handle.Grip;
            }

            if (Distance(pt, visual.RotateHandle) <= hitRadius)
                return HandleGrip.Rotate;

            return HandleGrip.None;
        }

        private static bool PointInPolygon(SKPoint pt, IReadOnlyList<SKPoint> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                var intersect = ((pi.Y > pt.Y) != (pj.Y > pt.Y)) &&
                                (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y + float.Epsilon) + pi.X);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        private void EnsureVisuals()
        {
            if (_visualCache.Count == 0)
            {
                UpdateVisualCache(_lastCanvasWidth == 0 ? Math.Max(1, _canvas.Width) : _lastCanvasWidth,
                    _lastCanvasHeight == 0 ? Math.Max(1, _canvas.Height) : _lastCanvasHeight,
                    _lastBaseDir);
            }
        }

        private void UpdateVisualCache(int baseWidth, int baseHeight, string baseDir)
        {
            _visualCache.Clear();
            _lastCanvasWidth = baseWidth;
            _lastCanvasHeight = baseHeight;
            _lastBaseDir = baseDir;

            foreach (var layer in _template.Layers)
            {
                if (!layer.Visible) continue;
                if (TryBuildVisualInfo(layer, baseWidth, baseHeight, baseDir, out var info))
                {
                    _visualCache[layer] = info;
                }
            }
        }

        private bool TryBuildVisualInfo(LayerBase layer, int baseWidth, int baseHeight, string baseDir, out LayerVisualInfo info)
        {
            info = default!;
            float contentWidth = 1f, contentHeight = 1f;
            switch (layer)
            {
                case TextLayer text:
                {
                    using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
                    var isBold = string.Equals(text.Style.FontWeight, "bold", StringComparison.OrdinalIgnoreCase);
                    var isItalic = string.Equals(text.Style.FontStyle, "italic", StringComparison.OrdinalIgnoreCase);
                    SKFontStyle fontStyle =
                        isBold && isItalic ? SKFontStyle.BoldItalic :
                        isBold ? SKFontStyle.Bold :
                        isItalic ? SKFontStyle.Italic :
                        SKFontStyle.Normal;
                    paint.Typeface = SKTypeface.FromFamilyName(text.Style.FontFamily, fontStyle);
                    paint.TextSize = text.Style.FontSize;
                    var bounds = new SKRect();
                    paint.MeasureText(text.Text ?? string.Empty, ref bounds);
                    contentWidth = Math.Max(1f, bounds.Width);
                    contentHeight = Math.Max(1f, bounds.Height);
                    break;
                }
                case ImageLayer image:
                {
                    var path = image.Source?.Value;
                    if (path is null)
                        return false;
                    if (image.Source.Type == "file")
                        path = Path.Combine(baseDir, path);
                    if (!File.Exists(path))
                        return false;
                    using var bmp = SKBitmap.Decode(path);
                    if (bmp is null)
                        return false;
                    contentWidth = Math.Max(1f, bmp.Width);
                    contentHeight = Math.Max(1f, bmp.Height);
                    break;
                }
                default:
                    return false;
            }

            var t = layer.Transform;
            var parsedX = Utils.TryParsePercentOrNumber(t.X, baseWidth, out var px) ? px : 0f;
            var parsedY = Utils.TryParsePercentOrNumber(t.Y, baseHeight, out var py) ? py : 0f;

            var width = contentWidth;
            var height = contentHeight;
            var widthAuto = true;
            var heightAuto = true;

            if (!string.IsNullOrWhiteSpace(t.Width) && Utils.TryParsePercentOrNumber(t.Width!, baseWidth, out var w))
            {
                width = Math.Max(1f, w);
                widthAuto = false;
            }

            if (!string.IsNullOrWhiteSpace(t.Height) && Utils.TryParsePercentOrNumber(t.Height!, baseHeight, out var h))
            {
                height = Math.Max(1f, h);
                heightAuto = false;
            }

            var matrix = SKMatrix.CreateTranslation(GetAnchorOffsetX(t.Anchor, width), GetAnchorOffsetY(t.Anchor, height));
            matrix = matrix.PreConcat(SKMatrix.CreateScale(width / contentWidth, height / contentHeight));
            matrix = matrix.PreConcat(SKMatrix.CreateRotationDegrees(t.Rotation));
            matrix = matrix.PreConcat(SKMatrix.CreateTranslation(parsedX, parsedY));

            var points = new[]
            {
                new SKPoint(0,0),
                new SKPoint(contentWidth,0),
                new SKPoint(contentWidth,contentHeight),
                new SKPoint(0,contentHeight)
            };
            matrix.MapPoints(points);

            var widthVec = Subtract(points[1], points[0]);
            var heightVec = Subtract(points[3], points[0]);
            var widthLength = Math.Max(1f, Length(widthVec));
            var heightLength = Math.Max(1f, Length(heightVec));
            var ux = Normalize(widthVec);
            var uy = Normalize(heightVec);
            var center = new SKPoint((points[0].X + points[2].X) / 2f, (points[0].Y + points[2].Y) / 2f);
            var topCenter = new SKPoint((points[0].X + points[1].X) / 2f, (points[0].Y + points[1].Y) / 2f);
            var rightCenter = new SKPoint((points[1].X + points[2].X) / 2f, (points[1].Y + points[2].Y) / 2f);
            var bottomCenter = new SKPoint((points[2].X + points[3].X) / 2f, (points[2].Y + points[3].Y) / 2f);
            var leftCenter = new SKPoint((points[3].X + points[0].X) / 2f, (points[3].Y + points[0].Y) / 2f);
            var rotateBase = topCenter;
            var rotateDir = Normalize(Subtract(topCenter, center));
            if (Length(rotateDir) < 0.001f)
                rotateDir = new SKPoint(0, -1);
            var rotateHandle = new SKPoint(rotateBase.X + rotateDir.X * 24f, rotateBase.Y + rotateDir.Y * 24f);

            var handles = new List<HandlePosition>
            {
                new HandlePosition(HandleGrip.TopLeft, points[0]),
                new HandlePosition(HandleGrip.Top, topCenter),
                new HandlePosition(HandleGrip.TopRight, points[1]),
                new HandlePosition(HandleGrip.Right, rightCenter),
                new HandlePosition(HandleGrip.BottomRight, points[2]),
                new HandlePosition(HandleGrip.Bottom, bottomCenter),
                new HandlePosition(HandleGrip.BottomLeft, points[3]),
                new HandlePosition(HandleGrip.Left, leftCenter)
            };

            info = new LayerVisualInfo(layer, matrix, points, center, topCenter, rightCenter, bottomCenter, leftCenter,
                rotateBase, rotateHandle, ux, uy, widthLength, heightLength, parsedX, parsedY, width, height,
                widthAuto, heightAuto, t.Rotation, contentWidth, contentHeight, baseWidth, baseHeight, handles);
            return true;
        }

        private void DrawSelection(SKCanvas canvas, LayerVisualInfo visual)
        {
            // Get DPI scaling factor for high DPI support
            var dpiScale = DeviceDpi / 96f;
            var handleSize = 6f;  // Base size in logical pixels
            var handleStroke = 1.5f;  // Base stroke width
            
            using var outline = new SKPaint { Color = new SKColor(102, 167, 207), IsAntialias = true, StrokeWidth = handleStroke * dpiScale, Style = SKPaintStyle.Stroke };
            using var fill = new SKPaint { Color = new SKColor(102, 167, 207, 160), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var fillHover = new SKPaint { Color = new SKColor(102, 167, 207, 220), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var fillWhite = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var outlineHandle = new SKPaint { Color = new SKColor(60, 60, 60), IsAntialias = true, StrokeWidth = 1f * dpiScale, Style = SKPaintStyle.Stroke };
            using var line = new SKPaint { Color = new SKColor(102, 167, 207, 160), IsAntialias = true, StrokeWidth = 1f * dpiScale, Style = SKPaintStyle.Stroke };

            using var path = new SKPath();
            path.MoveTo(visual.Corners[0]);
            for (int i = 1; i < visual.Corners.Length; i++)
                path.LineTo(visual.Corners[i]);
            path.Close();
            canvas.DrawPath(path, outline);

            // Draw handles with hover highlight
            foreach (var handle in visual.Handles)
            {
                var isHovered = _hoverGrip == handle.Grip;
                var rect = new SKRect(
                    handle.Position.X - handleSize * dpiScale, 
                    handle.Position.Y - handleSize * dpiScale,
                    handle.Position.X + handleSize * dpiScale, 
                    handle.Position.Y + handleSize * dpiScale);
                canvas.DrawRect(rect, fillWhite);
                canvas.DrawRect(rect, outlineHandle);
                if (isHovered)
                {
                    var hoverRect = new SKRect(
                        handle.Position.X - (handleSize + 2) * dpiScale,
                        handle.Position.Y - (handleSize + 2) * dpiScale,
                        handle.Position.X + (handleSize + 2) * dpiScale,
                        handle.Position.Y + (handleSize + 2) * dpiScale);
                    canvas.DrawRect(hoverRect, fillHover);
                    canvas.DrawRect(rect, fillWhite);
                    canvas.DrawRect(rect, outlineHandle);
                }
            }

            // Draw rotate handle with line
            canvas.DrawLine(visual.RotateBase, visual.RotateHandle, line);
            var isRotateHovered = _hoverGrip == HandleGrip.Rotate;
            var rotateRadius = handleSize * dpiScale;
            if (isRotateHovered)
            {
                canvas.DrawCircle(visual.RotateHandle, rotateRadius + 2f * dpiScale, fillHover);
            }
            canvas.DrawCircle(visual.RotateHandle, rotateRadius, fillWhite);
            canvas.DrawCircle(visual.RotateHandle, rotateRadius, outlineHandle);
        }

        private void ApplyMove(SKPoint current)
        {
            if (_selected == null || _dragState == null) return;
            var dx = current.X - _dragState.InitialMouse.X;
            var dy = current.Y - _dragState.InitialMouse.Y;

            var newX = _dragState.InitialX + dx;
            var newY = _dragState.InitialY + dy;
            _selected.Transform.X = FormatNumber(newX);
            _selected.Transform.Y = FormatNumber(newY);
        }

        private void ApplyRotate(SKPoint current)
        {
            if (_selected == null || _dragState == null) return;
            var center = _dragState.Visual.Center;
            var v0 = Subtract(_dragState.InitialMouse, center);
            var v1 = Subtract(current, center);
            if (Length(v0) < 0.001f || Length(v1) < 0.001f) return;
            var startAngle = MathF.Atan2(v0.Y, v0.X);
            var currentAngle = MathF.Atan2(v1.Y, v1.X);
            var delta = (currentAngle - startAngle) * (180f / MathF.PI);
            var newRotation = _dragState.InitialRotation + delta;
            
            // Shift: snap to 15° increments
            if ((ModifierKeys & Keys.Shift) != 0)
            {
                newRotation = MathF.Round(newRotation / 15f) * 15f;
            }
            
            _selected.Transform.Rotation = newRotation;
        }

        private void ApplyScale(SKPoint current, HandleGrip grip)
        {
            if (_selected == null || _dragState == null) return;
            var info = _dragState.Visual;
            var useCenter = (ModifierKeys & Keys.Alt) != 0;
            var pivot = GetPivot(info, grip, useCenter);
            var widthDir = GetWidthDirection(info, grip);
            var heightDir = GetHeightDirection(info, grip);
            var affectsWidth = widthDir.HasValue;
            var affectsHeight = heightDir.HasValue;

            float widthFactor = useCenter ? 2f : 1f;
            float heightFactor = useCenter ? 2f : 1f;

            var v = Subtract(current, pivot);
            var newWidth = _dragState.InitialWidth;
            var newHeight = _dragState.InitialHeight;

            if (affectsWidth)
            {
                var dir = widthDir!.Value;
                var projection = Dot(v, dir);
                newWidth = Math.Max(4f, Math.Abs(projection) * widthFactor);
            }

            if (affectsHeight)
            {
                var dir = heightDir!.Value;
                var projection = Dot(v, dir);
                newHeight = Math.Max(4f, Math.Abs(projection) * heightFactor);
            }

            var shift = (ModifierKeys & Keys.Shift) != 0;
            if (shift)
            {
                var ratio = _dragState.InitialWidth / _dragState.InitialHeight;
                if (affectsWidth && !affectsHeight)
                {
                    newHeight = Math.Max(4f, newWidth / ratio);
                    affectsHeight = true;
                }
                else if (!affectsWidth && affectsHeight)
                {
                    newWidth = Math.Max(4f, newHeight * ratio);
                    affectsWidth = true;
                }
                else if (affectsWidth && affectsHeight && ratio > 0)
                {
                    var widthDelta = Math.Abs(newWidth - _dragState.InitialWidth);
                    var heightDelta = Math.Abs(newHeight - _dragState.InitialHeight);
                    if (widthDelta >= heightDelta)
                        newHeight = Math.Max(4f, newWidth / ratio);
                    else
                        newWidth = Math.Max(4f, newHeight * ratio);
                }
            }

            var newCorners = ReconstructCorners(info, grip, pivot, newWidth, newHeight, useCenter);
            ApplyGeometryToTransform(_selected.Transform, info, newCorners, affectsWidth, affectsHeight);
        }

        private SKPoint[] ReconstructCorners(LayerVisualInfo info, HandleGrip grip, SKPoint pivot, float newWidth, float newHeight, bool useCenter)
        {
            var ux = info.Ux;
            var uy = info.Uy;
            var corners = new SKPoint[4];

            if (useCenter)
            {
                var halfW = Multiply(ux, newWidth / 2f);
                var halfH = Multiply(uy, newHeight / 2f);
                corners[0] = Subtract(Subtract(pivot, halfW), halfH);
                corners[1] = Add(Subtract(pivot, halfH), halfW);
                corners[2] = Add(Add(pivot, halfW), halfH);
                corners[3] = Add(Subtract(pivot, halfW), halfH);
                return corners;
            }

            switch (grip)
            {
                case HandleGrip.TopLeft:
                {
                    var newTL = Add(Add(pivot, Multiply(ux, -newWidth)), Multiply(uy, -newHeight));
                    corners[0] = newTL;
                    corners[1] = Add(newTL, Multiply(ux, newWidth));
                    corners[3] = Add(newTL, Multiply(uy, newHeight));
                    corners[2] = Add(corners[1], Multiply(uy, newHeight));
                    break;
                }
                case HandleGrip.TopRight:
                {
                    var newTL = Add(pivot, Multiply(uy, -newHeight));
                    corners[0] = newTL;
                    corners[1] = Add(newTL, Multiply(ux, newWidth));
                    corners[3] = pivot;
                    corners[2] = Add(corners[1], Multiply(uy, newHeight));
                    break;
                }
                case HandleGrip.BottomRight:
                {
                    var newTL = pivot;
                    corners[0] = newTL;
                    corners[1] = Add(newTL, Multiply(ux, newWidth));
                    corners[3] = Add(newTL, Multiply(uy, newHeight));
                    corners[2] = Add(corners[1], Multiply(uy, newHeight));
                    break;
                }
                case HandleGrip.BottomLeft:
                {
                    var newTL = Add(pivot, Multiply(ux, -newWidth));
                    corners[0] = newTL;
                    corners[1] = pivot;
                    corners[3] = Add(newTL, Multiply(uy, newHeight));
                    corners[2] = Add(corners[1], Multiply(uy, newHeight));
                    break;
                }
                case HandleGrip.Top:
                {
                    var bottomCenter = info.BottomCenter;
                    var halfWidth = Multiply(ux, info.Width / 2f);
                    var topCenter = Add(bottomCenter, Multiply(uy, -newHeight));
                    corners[0] = Subtract(topCenter, halfWidth);
                    corners[1] = Add(topCenter, halfWidth);
                    corners[2] = Add(bottomCenter, halfWidth);
                    corners[3] = Subtract(bottomCenter, halfWidth);
                    break;
                }
                case HandleGrip.Bottom:
                {
                    var topCenter = info.TopCenter;
                    var halfWidth = Multiply(ux, info.Width / 2f);
                    var bottomCenter = Add(topCenter, Multiply(uy, newHeight));
                    corners[0] = Subtract(topCenter, halfWidth);
                    corners[1] = Add(topCenter, halfWidth);
                    corners[2] = Add(bottomCenter, halfWidth);
                    corners[3] = Subtract(bottomCenter, halfWidth);
                    break;
                }
                case HandleGrip.Right:
                {
                    var leftCenter = info.LeftCenter;
                    var halfHeight = Multiply(uy, info.Height / 2f);
                    var rightCenter = Add(leftCenter, Multiply(ux, newWidth));
                    corners[0] = Subtract(leftCenter, halfHeight);
                    corners[3] = Add(leftCenter, halfHeight);
                    corners[1] = Subtract(rightCenter, halfHeight);
                    corners[2] = Add(rightCenter, halfHeight);
                    break;
                }
                case HandleGrip.Left:
                {
                    var rightCenter = info.RightCenter;
                    var halfHeight = Multiply(uy, info.Height / 2f);
                    var leftCenter = Add(rightCenter, Multiply(ux, -newWidth));
                    corners[0] = Subtract(leftCenter, halfHeight);
                    corners[3] = Add(leftCenter, halfHeight);
                    corners[1] = Subtract(rightCenter, halfHeight);
                    corners[2] = Add(rightCenter, halfHeight);
                    break;
                }
            }

            return corners;
        }

        private void ApplyGeometryToTransform(TransformSpec transform, LayerVisualInfo info, SKPoint[] corners, bool affectsWidth, bool affectsHeight)
        {
            var widthVec = Subtract(corners[1], corners[0]);
            var heightVec = Subtract(corners[3], corners[0]);
            var newWidth = Length(widthVec);
            var newHeight = Length(heightVec);
            var rotation = MathF.Atan2(widthVec.Y, widthVec.X) * (180f / MathF.PI);

            var anchorPoint = GetAnchorPoint(info.Transform.Anchor, corners);

            transform.X = FormatNumber(anchorPoint.X);
            transform.Y = FormatNumber(anchorPoint.Y);
            transform.Rotation = rotation;

            if (affectsWidth || !info.WidthAuto)
                transform.Width = FormatNumber(newWidth);
            if (affectsHeight || !info.HeightAuto)
                transform.Height = FormatNumber(newHeight);
        }

        private static SKPoint GetAnchorPoint(Core.Anchor anchor, IReadOnlyList<SKPoint> corners)
        {
            var tl = corners[0];
            var tr = corners[1];
            var br = corners[2];
            var bl = corners[3];
            return anchor switch
            {
                Core.Anchor.TL => tl,
                Core.Anchor.TC => new SKPoint((tl.X + tr.X) / 2f, (tl.Y + tr.Y) / 2f),
                Core.Anchor.TR => tr,
                Core.Anchor.CL => new SKPoint((tl.X + bl.X) / 2f, (tl.Y + bl.Y) / 2f),
                Core.Anchor.CC => new SKPoint((tl.X + br.X) / 2f, (tl.Y + br.Y) / 2f),
                Core.Anchor.CR => new SKPoint((tr.X + br.X) / 2f, (tr.Y + br.Y) / 2f),
                Core.Anchor.BL => bl,
                Core.Anchor.BC => new SKPoint((bl.X + br.X) / 2f, (bl.Y + br.Y) / 2f),
                Core.Anchor.BR => br,
                _ => tl
            };
        }

        private static SKPoint GetPivot(LayerVisualInfo info, HandleGrip grip, bool useCenter)
        {
            if (useCenter)
                return info.Center;

            return grip switch
            {
                HandleGrip.TopLeft => info.Corners[2],
                HandleGrip.TopRight => info.Corners[3],
                HandleGrip.BottomRight => info.Corners[0],
                HandleGrip.BottomLeft => info.Corners[1],
                HandleGrip.Top => info.BottomCenter,
                HandleGrip.Bottom => info.TopCenter,
                HandleGrip.Left => info.RightCenter,
                HandleGrip.Right => info.LeftCenter,
                _ => info.Center
            };
        }

        private static SKPoint? GetWidthDirection(LayerVisualInfo info, HandleGrip grip)
        {
            return grip switch
            {
                HandleGrip.TopLeft => Multiply(info.Ux, -1f),
                HandleGrip.TopRight => info.Ux,
                HandleGrip.BottomRight => info.Ux,
                HandleGrip.BottomLeft => Multiply(info.Ux, -1f),
                HandleGrip.Left => Multiply(info.Ux, -1f),
                HandleGrip.Right => info.Ux,
                _ => null
            };
        }

        private static SKPoint? GetHeightDirection(LayerVisualInfo info, HandleGrip grip)
        {
            return grip switch
            {
                HandleGrip.TopLeft => Multiply(info.Uy, -1f),
                HandleGrip.TopRight => Multiply(info.Uy, -1f),
                HandleGrip.BottomRight => info.Uy,
                HandleGrip.BottomLeft => info.Uy,
                HandleGrip.Top => Multiply(info.Uy, -1f),
                HandleGrip.Bottom => info.Uy,
                _ => null
            };
        }

        private static float GetAnchorOffsetX(Core.Anchor anchor, float width)
        {
            return anchor switch
            {
                Core.Anchor.TL or Core.Anchor.CL or Core.Anchor.BL => 0f,
                Core.Anchor.TC or Core.Anchor.CC or Core.Anchor.BC => -width / 2f,
                Core.Anchor.TR or Core.Anchor.CR or Core.Anchor.BR => -width,
                _ => 0f
            };
        }

        private static float GetAnchorOffsetY(Core.Anchor anchor, float height)
        {
            return anchor switch
            {
                Core.Anchor.TL or Core.Anchor.TC or Core.Anchor.TR => 0f,
                Core.Anchor.CL or Core.Anchor.CC or Core.Anchor.CR => -height / 2f,
                Core.Anchor.BL or Core.Anchor.BC or Core.Anchor.BR => -height,
                _ => 0f
            };
        }

        private static float Distance(SKPoint a, SKPoint b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private static SKPoint Subtract(SKPoint a, SKPoint b) => new(a.X - b.X, a.Y - b.Y);
        private static SKPoint Add(SKPoint a, SKPoint b) => new(a.X + b.X, a.Y + b.Y);
        private static SKPoint Multiply(SKPoint v, float s) => new(v.X * s, v.Y * s);
        private static float Dot(SKPoint a, SKPoint b) => a.X * b.X + a.Y * b.Y;
        private static float Length(SKPoint v) => MathF.Sqrt(MathF.Max(0f, v.X * v.X + v.Y * v.Y));
        private static SKPoint Normalize(SKPoint v)
        {
            var len = Length(v);
            if (len < 0.0001f) return new SKPoint(0, 0);
            return new SKPoint(v.X / len, v.Y / len);
        }

        private static string FormatNumber(float value)
        {
            return Math.Round(value, 2).ToString("0.##");
        }

        private readonly struct HandlePosition
        {
            public HandlePosition(HandleGrip grip, SKPoint position)
            {
                Grip = grip;
                Position = position;
            }

            public HandleGrip Grip { get; }
            public SKPoint Position { get; }
        }

        private sealed class LayerVisualInfo
        {
            public LayerVisualInfo(LayerBase layer, SKMatrix matrix, SKPoint[] corners, SKPoint center,
                SKPoint topCenter, SKPoint rightCenter, SKPoint bottomCenter, SKPoint leftCenter,
                SKPoint rotateBase, SKPoint rotateHandle, SKPoint ux, SKPoint uy, float width, float height,
                float parsedX, float parsedY, float finalWidth, float finalHeight, bool widthAuto, bool heightAuto,
                float rotation, float contentWidth, float contentHeight, int baseWidth, int baseHeight,
                List<HandlePosition> handles)
            {
                Layer = layer;
                Matrix = matrix;
                Corners = corners;
                Center = center;
                TopCenter = topCenter;
                RightCenter = rightCenter;
                BottomCenter = bottomCenter;
                LeftCenter = leftCenter;
                RotateBase = rotateBase;
                RotateHandle = rotateHandle;
                Ux = ux;
                Uy = uy;
                Width = width;
                Height = height;
                ParsedX = parsedX;
                ParsedY = parsedY;
                FinalWidth = finalWidth;
                FinalHeight = finalHeight;
                WidthAuto = widthAuto;
                HeightAuto = heightAuto;
                Transform = layer.Transform;
                ContentWidth = contentWidth;
                ContentHeight = contentHeight;
                BaseWidth = baseWidth;
                BaseHeight = baseHeight;
                Handles = handles;
                Rotation = rotation;
            }

            public LayerBase Layer { get; }
            public SKMatrix Matrix { get; }
            public SKPoint[] Corners { get; }
            public SKPoint Center { get; }
            public SKPoint TopCenter { get; }
            public SKPoint RightCenter { get; }
            public SKPoint BottomCenter { get; }
            public SKPoint LeftCenter { get; }
            public SKPoint RotateBase { get; }
            public SKPoint RotateHandle { get; }
            public SKPoint Ux { get; }
            public SKPoint Uy { get; }
            public float Width { get; }
            public float Height { get; }
            public float ParsedX { get; }
            public float ParsedY { get; }
            public float FinalWidth { get; }
            public float FinalHeight { get; }
            public bool WidthAuto { get; }
            public bool HeightAuto { get; }
            public TransformSpec Transform { get; }
            public float ContentWidth { get; }
            public float ContentHeight { get; }
            public int BaseWidth { get; }
            public int BaseHeight { get; }
            public IReadOnlyList<HandlePosition> Handles { get; }
            public float Rotation { get; }
            public float HandleVisualRadius => 6f;
            public float HandleHitRadius => 10f;
        }

        private sealed class DragState
        {
            public required SKPoint InitialMouse { get; init; }
            public required LayerVisualInfo Visual { get; init; }
            public required SKPoint[] InitialCorners { get; init; }
            public required float InitialWidth { get; init; }
            public required float InitialHeight { get; init; }
            public required float InitialRotation { get; init; }
            public required float InitialX { get; init; }
            public required float InitialY { get; init; }
            public required bool WidthWasAuto { get; init; }
            public required bool HeightWasAuto { get; init; }
            public required SKPoint InitialCenter { get; init; }
            public required float BaseWidth { get; init; }
            public required float BaseHeight { get; init; }
        }

        private enum HandleGrip
        {
            None,
            Body,
            Rotate,
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left
        }
    }
}
