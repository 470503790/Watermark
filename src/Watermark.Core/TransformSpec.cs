using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class TransformSpec
{
    [JsonProperty("x")] public string X { get; set; } = "0"; // number or percent string
    [JsonProperty("y")] public string Y { get; set; } = "0";
    [JsonProperty("width")] public string? Width { get; set; } // null = auto
    [JsonProperty("height")] public string? Height { get; set; }
    [JsonProperty("rotation")] public float Rotation { get; set; } = 0f;
    [JsonProperty("anchor")] public Anchor Anchor { get; set; } = Anchor.TL;
    [JsonProperty("relativeTo")] public string RelativeTo { get; set; } = "canvas"; // canvas|shorterSide|longerSide|imageBounds
}
