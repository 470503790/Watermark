using Newtonsoft.Json;

namespace Watermark.Core;

public sealed class TilingSpec
{
    [JsonProperty("enabled")] public bool Enabled { get; set; } = false;
    [JsonProperty("angle")] public float Angle { get; set; } = 0f;
    [JsonProperty("spacingX")] public string SpacingX { get; set; } = "0";
    [JsonProperty("spacingY")] public string SpacingY { get; set; } = "0";
    [JsonProperty("offset")] public float Offset { get; set; } = 0f;
}
