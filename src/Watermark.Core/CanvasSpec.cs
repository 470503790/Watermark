using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class CanvasSpec
{
    [JsonProperty("width")] public int Width { get; set; }
    [JsonProperty("height")] public int Height { get; set; }
    [JsonProperty("dpi")] public int Dpi { get; set; } = 96;
    [JsonProperty("background")] public BackgroundSpec Background { get; set; } = new();

    public sealed class BackgroundSpec
    {
        [JsonProperty("type")] public string Type { get; set; } = "transparent"; // transparent|color|image
        [JsonProperty("value")] public string? Value { get; set; } // hex color or image path
    }
}
