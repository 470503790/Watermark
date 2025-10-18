using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class ImageLayer : LayerBase
{
    public override string Type => nameof(ImageLayer);
    [JsonProperty("source")] public ImageSource Source { get; set; } = new();
    [JsonProperty("sizing")] public ImageSizing Sizing { get; set; } = ImageSizing.None;
    [JsonProperty("cornerRadius")] public float CornerRadius { get; set; } = 0f;
    [JsonProperty("tintColor")] public string? TintColor { get; set; }
}

public sealed class ImageSource
{
    [JsonProperty("type")] public string Type { get; set; } = "file"; // file|embedded|expr
    [JsonProperty("value")] public string? Value { get; set; }
}
