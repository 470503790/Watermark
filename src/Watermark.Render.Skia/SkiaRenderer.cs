using SkiaSharp;
using Watermark.Core;

namespace Watermark.Render.Skia;

public sealed class SkiaRenderer : IRenderer
{
    public void RenderToFile(Template template, string inputImagePath, string outputPath, ExportSettings? overrideSettings = null)
    {
        var settings = overrideSettings ?? template.Export;
        using var inputBitmap = SKBitmap.Decode(inputImagePath) ?? throw new InvalidOperationException("Failed to decode input image.");
        var width = inputBitmap.Width;
        var height = inputBitmap.Height;

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(inputBitmap, 0, 0);

        DrawLayers(canvas, template, width, height, Path.GetDirectoryName(inputImagePath) ?? Environment.CurrentDirectory);

        using var snapshot = surface.Snapshot();
        using var data = settings.Format.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => snapshot.Encode(SKEncodedImageFormat.Jpeg, settings.JpegQuality),
            "webp" => snapshot.Encode(SKEncodedImageFormat.Webp, settings.JpegQuality),
            _ => snapshot.Encode(SKEncodedImageFormat.Png, 100),
        };
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var fs = File.Open(outputPath, FileMode.Create, FileAccess.Write);
        data.SaveTo(fs);
    }

    public static void DrawLayers(SKCanvas canvas, Template template, int baseWidth, int baseHeight, string baseDir)
    {
        foreach (var layer in template.Layers)
        {
            if (!layer.Visible || layer.Opacity <= 0) continue;
            switch (layer)
            {
                case TextLayer tl:
                    DrawTextLayer(canvas, tl, baseWidth, baseHeight);
                    break;
                case ImageLayer il:
                    DrawImageLayer(canvas, il, baseWidth, baseHeight, baseDir);
                    break;
            }
        }
    }

    private static void ApplyTransform(SKCanvas canvas, TransformSpec t, float contentWidth, float contentHeight, int baseWidth, int baseHeight)
    {
        float x = 0, y = 0, w = contentWidth, h = contentHeight;
        if (Utils.TryParsePercentOrNumber(t.X, baseWidth, out var px)) x = px;
        if (Utils.TryParsePercentOrNumber(t.Y, baseHeight, out var py)) y = py;
        if (t.Width != null && Utils.TryParsePercentOrNumber(t.Width, baseWidth, out var pw)) w = pw;
        if (t.Height != null && Utils.TryParsePercentOrNumber(t.Height, baseHeight, out var ph)) h = ph;

        // Anchor offset
        float ax = 0, ay = 0;
        ax = t.Anchor switch
        {
            Anchor.TL or Anchor.CL or Anchor.BL => 0,
            Anchor.TC or Anchor.CC or Anchor.BC => -w / 2f,
            Anchor.TR or Anchor.CR or Anchor.BR => -w,
            _ => 0
        };
        ay = t.Anchor switch
        {
            Anchor.TL or Anchor.TC or Anchor.TR => 0,
            Anchor.CL or Anchor.CC or Anchor.CR => -h / 2f,
            Anchor.BL or Anchor.BC or Anchor.BR => -h,
            _ => 0
        };

        var matrix = SKMatrix.CreateTranslation(ax, ay);
        matrix = matrix.PreConcat(SKMatrix.CreateScale(w / contentWidth, h / contentHeight));
        matrix = matrix.PreConcat(SKMatrix.CreateRotationDegrees(t.Rotation));
        matrix = matrix.PreConcat(SKMatrix.CreateTranslation(x, y));
        canvas.SetMatrix(matrix.PostConcat(canvas.TotalMatrix));
    }

    private static SKColor ParseColor(string hex, byte? overrideAlpha = null)
    {
        var c = SKColor.Parse(hex);
        if (overrideAlpha.HasValue) c = c.WithAlpha(overrideAlpha.Value);
        return c;
    }

    private static void DrawTextLayer(SKCanvas canvas, TextLayer layer, int baseW, int baseH)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = ParseColor(layer.Style.FillColor) };

        // 处理字体样式 (粗体 / 斜体 组合)
        var isBold = string.Equals(layer.Style.FontWeight, "bold", StringComparison.OrdinalIgnoreCase);
        var isItalic = string.Equals(layer.Style.FontStyle, "italic", StringComparison.OrdinalIgnoreCase);

        SKFontStyle fontStyle =
            isBold && isItalic ? SKFontStyle.BoldItalic :
            isBold ? SKFontStyle.Bold :
            isItalic ? SKFontStyle.Italic :
            SKFontStyle.Normal;

        paint.Typeface = SKTypeface.FromFamilyName(layer.Style.FontFamily, fontStyle);
        paint.TextSize = layer.Style.FontSize;

        var text = layer.Text ?? string.Empty;
        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);
        float contentW = bounds.Width;
        float contentH = bounds.Height;

        canvas.Save();
        ApplyTransform(canvas, layer.Transform, Math.Max(contentW, 1), Math.Max(contentH, 1), baseW, baseH);

        float tx = 0, ty = -bounds.Top; // 让文本顶部对齐
        if (layer.Style.MaxWidth.HasValue)
        {
            contentW = Math.Min(contentW, layer.Style.MaxWidth.Value);
        }

        canvas.DrawText(text, tx, ty, paint);
        canvas.Restore();
    }

    private static void DrawImageLayer(SKCanvas canvas, ImageLayer layer, int baseW, int baseH, string baseDir)
    {
        if (layer.Source?.Value is null) return;
        var path = layer.Source.Type == "file" ? Path.Combine(baseDir, layer.Source.Value) : layer.Source.Value;
        if (!File.Exists(path)) return;
        using var bmp = SKBitmap.Decode(path);
        if (bmp is null) return;

        float contentW = bmp.Width;
        float contentH = bmp.Height;
        canvas.Save();
        ApplyTransform(canvas, layer.Transform, contentW, contentH, baseW, baseH);
        var dest = new SKRect(0, 0, contentW, contentH);
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium, Color = SKColors.White.WithAlpha((byte)(layer.Opacity * 255)) };
        if (layer.TintColor is not null)
        {
            paint.ColorFilter = SKColorFilter.CreateBlendMode(ParseColor(layer.TintColor), SKBlendMode.SrcIn);
        }
        canvas.DrawBitmap(bmp, dest, paint);
        canvas.Restore();
    }
}
