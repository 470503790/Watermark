using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class ExportSettings
{
    [JsonProperty("format")] public string Format { get; set; } = "png"; // png|jpg|webp
    [JsonProperty("jpegQuality")] public int JpegQuality { get; set; } = 90;
    [JsonProperty("pngCompression")] public int PngCompression { get; set; } = 6;
    [JsonProperty("colorSpace")] public string ColorSpace { get; set; } = "sRGB";
    [JsonProperty("stripMetadata")] public bool StripMetadata { get; set; } = false;
    [JsonProperty("fileNameTemplate")] public string FileNameTemplate { get; set; } = "{filename}_wm.png";
}
