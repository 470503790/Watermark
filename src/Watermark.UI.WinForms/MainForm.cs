using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
        }

        private bool _dragging = false;
        private System.Drawing.Point _last;
        private string _dragMode = "move"; // move|rotate|scale
        private void CanvasOnMouseDown(object? sender, MouseEventArgs e)
        {
            _last = e.Location;
            _selected = HitTest(e.Location);
            if (_selected != null) _dragging = true;
            _dragMode = e.Button == MouseButtons.Right ? "rotate" : "move";
        }
        private void CanvasOnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging || _selected == null) return;
            var dx = e.X - _last.X;
            var dy = e.Y - _last.Y;
            _last = e.Location;

            if (_dragMode == "move")
            {
                // Move in pixels
                float x = 0, y = 0;
                float.TryParse(_selected.Transform.X, out x);
                float.TryParse(_selected.Transform.Y, out y);
                _selected.Transform.X = (x + dx).ToString();
                _selected.Transform.Y = (y + dy).ToString();
            }
            else if (_dragMode == "rotate")
            {
                _selected.Transform.Rotation += dx * 0.5f;
            }
            _canvas.Invalidate();
        }
        private void CanvasOnMouseUp(object? sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private LayerBase? HitTest(System.Drawing.Point p)
        {
            // naive: pick top-most layer (reverse order) near its transform (not rotation-aware for M1)
            for (int i = _template.Layers.Count - 1; i >= 0; i--)
            {
                var l = _template.Layers[i];
                if (!l.Visible) continue;
                if (float.TryParse(l.Transform.X, out var x) && float.TryParse(l.Transform.Y, out var y))
                {
                    var rect = new System.Drawing.RectangleF(x - 10, y - 10, 200, 60);
                    if (rect.Contains(p)) return l;
                }
            }
            return null;
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
            SkiaRenderer.DrawLayers(canvas, _template, e.Info.Width, e.Info.Height, _currentImagePath != null ? Path.GetDirectoryName(_currentImagePath)! : Environment.CurrentDirectory);
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
                Transform = new TransformSpec { X = "100", Y = "100", Anchor = Anchor.TL }
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
                    Transform = new TransformSpec { X = "200", Y = "200", Anchor = Anchor.TL }
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
    }
}
